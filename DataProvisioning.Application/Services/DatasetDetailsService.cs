using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DataProvisioning.Application.DTOs;
using DataProvisioning.Application.Interfaces;
using DataProvisioning.Domain.Enums;
using DataProvisioning.Domain.Entities;

namespace DataProvisioning.Application.Services;

public class DatasetDetailsService : IDatasetDetailsService
{
    private readonly IApplicationDbContext _context;

    public DatasetDetailsService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DatasetDetailsDto?> GetDatasetDetailsAsync(int datasetId, int currentUserId, string currentUserRole)
    {
        var dataset = await _context.Datasets
            .Include(d => d.OwnerGroup)
                .ThenInclude(g => g != null ? g.Owner : null)
            .Include(d => d.Columns)
            .Include(d => d.Reports)
            .FirstOrDefaultAsync(d => d.Id == datasetId);

        if (dataset == null) return null;

        var dto = new DatasetDetailsDto
        {
            Id = dataset.Id,
            Name = dataset.Name,
            Type = dataset.Type.ToString(),
            Description = dataset.Description,
            GroupName = dataset.OwnerGroup?.Name,
            OwnerGroupId = dataset.OwnerGroupId,
            
            IsOwnerOrAdmin = currentUserRole == "Admin" || (dataset.OwnerGroup != null && dataset.OwnerGroup.OwnerId == currentUserId)
        };

        dto.Columns = dataset.Columns.Select(c => new DatasetColumnDto
        {
            Id = c.Id,
            Name = c.Name,
            DataType = c.DataType,
            Definition = c.Definition,
            IsPii = c.IsPii,
            SampleData = c.SampleData
        }).ToList();

        // Linked Reports
        dto.LinkedReports = dataset.Reports.Select(r => new DatasetReportDto
        {
            Id = r.Id,
            Name = r.Name,
            Url = r.Url
        }).ToList();

        // Get requests for this user
        var requests = await _context.AccessRequests
            .Include(ar => ar.PolicyGroup)
            .Include(ar => ar.ReviewedBy)
            .Where(ar => ar.DatasetId == datasetId && ar.UserId == currentUserId)
            .OrderByDescending(ar => ar.CreatedAt)
            .ToListAsync();

        dto.HasFullAccess = requests.Any(ar => ar.Status == RequestStatus.Approved && ar.PolicyGroupId == null);

        // Fetch Global Approvers
        var globalApprovers = await _context.Users
            .Where(u => u.Role == UserRole.Admin || u.Role == UserRole.IAO) // Assuming IAO instead of 'IAA' based on Domain model
            .Select(u => new DatasetApproverDto { Name = u.Name, RoleType = u.Role.ToString() })
            .ToListAsync();

        // Fetch Group Approvers
        var groupApprovers = new System.Collections.Generic.List<DatasetApproverDto>();
        if (dataset.OwnerGroup != null)
        {
            groupApprovers.Add(new DatasetApproverDto { Name = dataset.OwnerGroup.Owner!.Name, RoleType = "Owner" });
            
            var members = await _context.VirtualGroupMembers
                .Include(vgm => vgm.User)
                .Where(vgm => vgm.GroupId == dataset.OwnerGroupId)
                .Select(vgm => new DatasetApproverDto { Name = vgm.User.Name, RoleType = "Member" })
                .ToListAsync();
                
            groupApprovers.AddRange(members);
        }

        dto.OwnerGroupMembers = groupApprovers.OrderBy(a => a.RoleType == "Owner" ? 0 : 1).ThenBy(a => a.Name).ToList();

        foreach (var req in requests)
        {
            var reqDto = new DatasetRequestDto
            {
                Id = req.Id,
                Status = req.Status.ToString(),
                PolicyName = req.PolicyGroup?.Name ?? "Full Dataset",
                PolicyGroupId = req.PolicyGroupId,
                ReviewerName = req.ReviewedBy?.Name,
                CreatedAt = req.CreatedAt
            };

            if (req.Status == RequestStatus.Pending)
            {
                reqDto.GlobalApprovers = globalApprovers;
                reqDto.PendingApprovers = groupApprovers.OrderBy(a => a.RoleType == "Owner" ? 0 : 1).ThenBy(a => a.Name).ToList();
            }

            dto.UserRequests.Add(reqDto);
        }

        // Available policies to request
        var requestedOrActiveIds = requests
            .Where(r => r.Status == RequestStatus.Approved || r.Status == RequestStatus.Pending)
            .Select(r => r.PolicyGroupId)
            .ToList();

        var policies = await _context.AssetPolicyGroups
            .Where(p => p.DatasetId == datasetId)
            .ToListAsync();

        dto.AvailablePolicies = policies
            .Where(p => !requestedOrActiveIds.Contains(p.Id))
            .Select(p => new AssetPolicyGroupDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description
            }).ToList();

        // Add "Full Dataset" if not requested yet
        if (!requestedOrActiveIds.Contains(null))
        {
            dto.AvailablePolicies.Add(new AssetPolicyGroupDto { Id = 0, Name = "Full Dataset (Requires Admin Approval)", Description = "" });
        }

        return dto;
    }

    public async Task<EditDatasetDto?> GetEditDatasetAsync(int datasetId, int currentUserId, string currentUserRole)
    {
        var dataset = await _context.Datasets
            .Include(d => d.OwnerGroup)
            .FirstOrDefaultAsync(d => d.Id == datasetId);

        if (dataset == null) return null;

        // Security Check (same as PHP: Admin, or Owner of Group, or Member of Group)
        bool canEdit = currentUserRole == "Admin";
        if (!canEdit && dataset.OwnerGroupId.HasValue)
        {
            var isOwnerOrMember = await _context.VirtualGroups
                .Where(g => g.Id == dataset.OwnerGroupId.Value)
                .AnyAsync(g => g.OwnerId == currentUserId || g.Members.Any(m => m.UserId == currentUserId));
            canEdit = isOwnerOrMember;
        }

        if (!canEdit) return null; // Unauthorized

        var groups = await _context.VirtualGroups
            .OrderBy(g => g.Name)
            .Select(g => new VirtualGroupDto { Id = g.Id, Name = g.Name })
            .ToListAsync();

        return new EditDatasetDto
        {
            Id = dataset.Id,
            Name = dataset.Name,
            Description = dataset.Description,
            OwnerGroupId = dataset.OwnerGroupId,
            AvailableGroups = groups
        };
    }

    public async Task UpdateDatasetAsync(EditDatasetDto dto)
    {
        var dataset = await _context.Datasets.FindAsync(dto.Id);
        if (dataset != null)
        {
            dataset.Name = dto.Name;
            dataset.Description = dto.Description;
            dataset.OwnerGroupId = dto.OwnerGroupId > 0 ? dto.OwnerGroupId : null;
            await _context.SaveChangesAsync();
        }
    }

    public async Task AddReportAsync(int datasetId, string name, string url)
    {
        // Add report and link it to dataset
        var report = new Report { Name = name, Url = url };
        var dataset = await _context.Datasets.FindAsync(datasetId);
        
        if (dataset != null)
        {
            report.Datasets.Add(dataset);
            _context.Reports.Add(report);
            await _context.SaveChangesAsync();
        }
    }
}
