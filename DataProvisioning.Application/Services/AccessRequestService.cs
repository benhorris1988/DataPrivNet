using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DataProvisioning.Application.DTOs;
using DataProvisioning.Application.Interfaces;
using DataProvisioning.Domain.Entities;
using DataProvisioning.Domain.Enums;

namespace DataProvisioning.Application.Services;

public class AccessRequestService : IAccessRequestService
{
    private readonly IApplicationDbContext _context;
    private readonly IDataWarehouseDbContext _dwContext;

    public AccessRequestService(IApplicationDbContext context, IDataWarehouseDbContext dwContext)
    {
        _context = context;
        _dwContext = dwContext;
    }

    public async Task<List<MyRequestDto>> GetMyRequestsAsync(int userId)
    {
        var requests = await _context.AccessRequests
            .Include(r => r.Dataset)
                .ThenInclude(d => d.OwnerGroup)
                    .ThenInclude(g => g != null ? g.Owner : null)
            .Include(r => r.ReviewedBy)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var globalApprovers = await _context.Users
            .Where(u => u.Role == UserRole.Admin || u.Role == UserRole.IAO)
            .Select(u => new DatasetApproverDto { Name = u.Name, RoleType = u.Role.ToString() })
            .ToListAsync();

        var result = new List<MyRequestDto>();

        foreach (var req in requests)
        {
            var dto = new MyRequestDto
            {
                Id = req.Id,
                DatasetId = req.DatasetId,
                DatasetName = req.Dataset.Name,
                OwnerName = req.Dataset.OwnerGroup?.Owner?.Name,
                GroupName = req.Dataset.OwnerGroup?.Name,
                Status = req.Status.ToString(),
                ReviewerName = req.ReviewedBy?.Name,
                CreatedAt = req.CreatedAt
            };

            if (req.Status == RequestStatus.Pending)
            {
                dto.GlobalApprovers = globalApprovers;
                
                if (req.Dataset.OwnerGroup != null)
                {
                    dto.PendingApprovers.Add(new DatasetApproverDto { Name = req.Dataset.OwnerGroup.Owner!.Name, RoleType = "Owner" });
                    
                    var members = await _context.VirtualGroupMembers
                        .Include(vgm => vgm.User)
                        .Where(vgm => vgm.GroupId == req.Dataset.OwnerGroupId)
                        .Select(vgm => new DatasetApproverDto { Name = vgm.User.Name, RoleType = "Member" })
                        .ToListAsync();
                        
                    dto.PendingApprovers.AddRange(members);
                    dto.PendingApprovers = dto.PendingApprovers.OrderBy(a => a.RoleType == "Owner" ? 0 : 1).ThenBy(a => a.Name).ToList();
                }
            }
            
            result.Add(dto);
        }

        return result;
    }

    public async Task SubmitRequestAsync(int userId, SubmitRequestDto requestDto)
    {
        var newRequest = new AccessRequest
        {
            UserId = userId,
            DatasetId = requestDto.DatasetId,
            Justification = requestDto.Justification,
            PolicyGroupId = requestDto.PolicyGroupId,
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.AccessRequests.Add(newRequest);
        await _context.SaveChangesAsync();
    }

    public async Task CancelOrRemoveRequestAsync(int requestId, int userId)
    {
        var request = await _context.AccessRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.UserId == userId);

        if (request != null)
        {
            _context.AccessRequests.Remove(request);
            await _context.SaveChangesAsync();
        }
    }

    public async Task ProcessRequestAsync(int requestId, int adminId, string action, int? policyGroupId)
    {
        var request = await _context.AccessRequests
            .Include(r => r.Dataset)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null)
            return;

        if (action == "approve")
        {
            request.Status = RequestStatus.Approved;
            request.ReviewedById = adminId;
            request.ReviewedAt = DateTime.UtcNow;
            request.PolicyGroupId = policyGroupId;
        }
        else if (action == "reject")
        {
            request.Status = RequestStatus.Rejected;
            request.ReviewedById = adminId;
            request.ReviewedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        if (action == "approve")
        {
            try
            {
                var permissions = new List<(string Col, string Val)>();

                if (!policyGroupId.HasValue)
                {
                    permissions.Add(("Any", "ALL"));
                }
                else
                {
                    var conditions = await _context.AssetPolicyConditions
                        .Where(c => c.PolicyGroupId == policyGroupId.Value)
                        .ToListAsync();

                    foreach (var c in conditions)
                        permissions.Add((c.ColumnName, c.Value));
                }

                var existingPermissions = await _dwContext.PermissionsMap
                    .Where(p => p.UserID == request.User.Name && p.TableName == request.Dataset.Name)
                    .ToListAsync();

                if (existingPermissions.Any())
                    _dwContext.PermissionsMap.RemoveRange(existingPermissions);

                foreach (var p in permissions)
                {
                    _dwContext.PermissionsMap.Add(new DataProvisioning.Domain.Entities.PermissionMap
                    {
                        UserID = request.User.Name,
                        TableName = request.Dataset.Name,
                        ColumnID = p.Col,
                        AuthorizedValue = p.Val
                    });
                }

                await _dwContext.SaveChangesAsync();
            }
            catch
            {
                // DW provisioning failure does not roll back the approval
            }
        }
    }

    public async Task<ManageAccessViewModel> GetManageAccessDashboardAsync(int userId, string role)
    {
        var vm = new ManageAccessViewModel();

        // 1. My Datasets
        var myDatasetsQuery = _context.Datasets
            .Where(d => d.OwnerGroupId != null && _context.VirtualGroups.Any(vg => vg.Id == d.OwnerGroupId && vg.OwnerId == userId));
            
        var myDatasets = await myDatasetsQuery
            .Select(d => new ManagedDatasetDto
            {
                Id = d.Id,
                Name = d.Name,
                Type = d.Type.ToString(),
                Description = d.Description,
                PolicyCount = _context.AssetPolicyGroups.Count(pg => pg.DatasetId == d.Id),
                UserAccessCount = _context.AccessRequests.Where(ar => ar.DatasetId == d.Id && ar.Status == RequestStatus.Approved).Select(ar => ar.UserId).Distinct().Count(),
                IsMissingFromDataWarehouse = false // We mock this for now, normally we'd query INFORMATION_SCHEMA via ADO.NET
            })
            .ToListAsync();
            
        vm.MyDatasets = myDatasets;

        // 2. Pending Requests
        var pendingQuery = _context.AccessRequests
            .Include(r => r.User)
            .Include(r => r.Dataset)
                .ThenInclude(d => d.OwnerGroup)
                    .ThenInclude(g => g != null ? g.Owner : null)
            .Include(r => r.Dataset)
                .ThenInclude(d => d.PolicyGroups)
            .Where(r => r.Status == RequestStatus.Pending);

        // Apply restrictions for IAO
        if (role == UserRole.IAO.ToString())
        {
            var myGroupIds = await _context.VirtualGroups
                .Where(g => g.OwnerId == userId || _context.VirtualGroupMembers.Any(gm => gm.GroupId == g.Id && gm.UserId == userId))
                .Select(g => g.Id)
                .ToListAsync();

            pendingQuery = pendingQuery.Where(r => r.Dataset.OwnerGroupId != null && myGroupIds.Contains(r.Dataset.OwnerGroupId.Value));
        }

        var pendingRequestsList = await pendingQuery
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        foreach (var req in pendingRequestsList)
        {
            var dto = new PendingRequestDto
            {
                Id = req.Id,
                DatasetId = req.DatasetId,
                RequestorName = req.User.Name,
                DatasetName = req.Dataset.Name,
                CreatedAt = req.CreatedAt,
                Justification = req.Justification,
                RequestedFilters = req.RequestedRlsFilters,
                SelectedPolicyGroupId = req.PolicyGroupId,
                AvailablePolicies = req.Dataset.PolicyGroups.Select(pg => new PolicyGroupOptionDto { Id = pg.Id, Name = pg.Name }).ToList()
            };

            // Populate Approvers Group (similar to Details/MyRequests)
            if (req.Dataset.OwnerGroup != null)
            {
                dto.GroupApprovers.Add(new DatasetApproverDto { Name = req.Dataset.OwnerGroup.Owner!.Name, RoleType = "Owner" });
                
                var members = await _context.VirtualGroupMembers
                    .Include(vgm => vgm.User)
                    .Where(vgm => vgm.GroupId == req.Dataset.OwnerGroupId)
                    .Select(vgm => new DatasetApproverDto { Name = vgm.User.Name, RoleType = "Member" })
                    .ToListAsync();
                    
                dto.GroupApprovers.AddRange(members);
                dto.GroupApprovers = dto.GroupApprovers.OrderBy(a => a.RoleType == "Owner" ? 0 : 1).ThenBy(a => a.Name).ToList();
            }

            vm.PendingRequests.Add(dto);
        }

        // 3. History (Recent Decisions)
        var historyQuery = _context.AccessRequests
            .Include(r => r.User)
            .Include(r => r.Dataset)
            .Include(r => r.PolicyGroup)
            .Where(r => r.Status != RequestStatus.Pending);

        if (role == UserRole.IAO.ToString())
        {
             var myGroupIds = await _context.VirtualGroups
                .Where(g => g.OwnerId == userId || _context.VirtualGroupMembers.Any(gm => gm.GroupId == g.Id && gm.UserId == userId))
                .Select(g => g.Id)
                .ToListAsync();

            historyQuery = historyQuery.Where(r => r.Dataset.OwnerGroupId != null && myGroupIds.Contains(r.Dataset.OwnerGroupId.Value));
        }

        vm.RecentDecisions = await historyQuery
            .OrderByDescending(r => r.ReviewedAt)
            .Take(10)
            .Select(r => new RequestHistoryDto
            {
                ReviewedAt = r.ReviewedAt ?? DateTime.UtcNow,
                RequestorName = r.User.Name,
                DatasetName = r.Dataset.Name,
                Status = r.Status.ToString(),
                AppliedPolicyName = r.PolicyGroup != null ? r.PolicyGroup.Name : null
            })
            .ToListAsync();

        return vm;
    }

    // --- Policy Management ---

    public async Task<ManagePolicyViewModel?> GetManagePolicyViewModelAsync(int datasetId, int currentUserId, string role)
    {
        var dataset = await _context.Datasets
            .Include(d => d.Columns)
            .Include(d => d.OwnerGroup)
            .FirstOrDefaultAsync(d => d.Id == datasetId);

        if (dataset == null) return null;

        // Security check
        bool canManage = role == "Admin";
        if (!canManage && dataset.OwnerGroupId.HasValue)
        {
            var isOwnerOrMember = await _context.VirtualGroups
                .Where(g => g.Id == dataset.OwnerGroupId.Value)
                .AnyAsync(g => g.OwnerId == currentUserId || g.Members.Any(m => m.UserId == currentUserId));
            canManage = isOwnerOrMember;
        }

        if (!canManage) return null;

        var vm = new ManagePolicyViewModel
        {
            DatasetId = dataset.Id,
            DatasetName = dataset.Name,
            DatasetColumns = dataset.Columns.Select(c => new DatasetColumnDto
            {
                Id = c.Id,
                Name = c.Name,
                DataType = c.DataType,
                Definition = c.Definition,
                IsPii = c.IsPii,
                SampleData = c.SampleData
            }).ToList()
        };

        // Available Owners (IAO / Admin)
        vm.AvailableOwners = await _context.Users
            .Where(u => u.Role == UserRole.Admin || u.Role == UserRole.IAO)
            .Select(u => new UserDto { Id = u.Id, Name = u.Name })
            .ToListAsync();

        var policyGroups = await _context.AssetPolicyGroups
            .Include(g => g.Owner)
            .Include(g => g.Conditions)
            .Include(g => g.HiddenColumns)
            .Where(g => g.DatasetId == datasetId)
            .ToListAsync();

        foreach (var pg in policyGroups)
        {
            var pgDto = new PolicyGroupDetailDto
            {
                Id = pg.Id,
                Name = pg.Name,
                Description = pg.Description,
                OwnerName = pg.Owner?.Name ?? "Unassigned"
            };

            pgDto.Conditions = pg.Conditions.Select(c => new PolicyConditionDto
            {
                Id = c.Id,
                ColumnName = c.ColumnName,
                Operator = c.Operator,
                Value = c.Value
            }).ToList();

            pgDto.Columns = pg.HiddenColumns.Select(hc => new PolicyColumnDto
            {
                ColumnName = hc.ColumnName,
                IsHidden = hc.IsHidden
            }).ToList();

            // Authorized Users (Status = Approved)
            var users = await _context.AccessRequests
                .Include(r => r.User)
                .Where(r => r.PolicyGroupId == pg.Id && r.Status == RequestStatus.Approved)
                .Select(r => new PolicyUserDto
                {
                    Name = r.User.Name,
                    Email = r.User.Email,
                    ReviewedAt = r.ReviewedAt
                })
                .ToListAsync();

            pgDto.AuthorizedUsers = users;

            vm.PolicyGroups.Add(pgDto);
        }

        return vm;
    }

    public async Task CreatePolicyGroupAsync(int datasetId, string name, string description, int? ownerId)
    {
        var group = new AssetPolicyGroup
        {
            DatasetId = datasetId,
            Name = name,
            Description = description,
            OwnerId = ownerId
        };
        _context.AssetPolicyGroups.Add(group);
        await _context.SaveChangesAsync();
    }

    public async Task AddPolicyConditionAsync(int policyGroupId, string columnName, string op, string value)
    {
        var condition = new AssetPolicyCondition
        {
            PolicyGroupId = policyGroupId,
            ColumnName = columnName,
            Operator = op,
            Value = value
        };
        _context.AssetPolicyConditions.Add(condition);
        await _context.SaveChangesAsync();
    }

    public async Task DeletePolicyConditionAsync(int conditionId)
    {
        var condition = await _context.AssetPolicyConditions.FindAsync(conditionId);
        if (condition != null)
        {
            _context.AssetPolicyConditions.Remove(condition);
            await _context.SaveChangesAsync();
        }
    }

    public async Task TogglePolicyColumnVisibilityAsync(int policyGroupId, string columnName, bool isVisible)
    {
        var existing = await _context.AssetPolicyColumns
            .FirstOrDefaultAsync(c => c.PolicyGroupId == policyGroupId && c.ColumnName == columnName);

        if (existing != null)
        {
            existing.IsHidden = !isVisible; // If passed true for isVisible, IsHidden = false
        }
        else
        {
            _context.AssetPolicyColumns.Add(new AssetPolicyColumn
            {
                PolicyGroupId = policyGroupId,
                ColumnName = columnName,
                IsHidden = !isVisible
            });
        }
        await _context.SaveChangesAsync();
    }
}
