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

public class AdministrationService : IAdministrationService
{
    private readonly IApplicationDbContext _context;

    public AdministrationService(IApplicationDbContext context)
    {
        _context = context;
    }

    // --- Users ---
    public async Task<List<UserDto>> GetUsersAsync()
    {
        return await _context.Users
            .OrderBy(u => u.Name)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Role = u.Role.ToString()
            })
            .ToListAsync();
    }

    public async Task AddUserAsync(string name, string email, string roleStr)
    {
        if (Enum.TryParse<UserRole>(roleStr, out var role))
        {
            _context.Users.Add(new User { Name = name, Email = email, Role = role });
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateUserRoleAsync(int userId, string roleStr)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null && Enum.TryParse<UserRole>(roleStr, out var role))
        {
            user.Role = role;
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateUserNameAsync(int userId, string name)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.Name = name;
            await _context.SaveChangesAsync();
        }
    }

    // --- Groups ---
    public async Task<List<VirtualGroupDto>> GetGroupsAsync(int currentUserId, string role)
    {
        IQueryable<VirtualGroup> query = _context.VirtualGroups
            .Include(g => g.Owner)
            .Include(g => g.Members)
                .ThenInclude(m => m.User);

        if (role != "Admin")
        {
            query = query.Where(g => g.OwnerId == currentUserId || g.Members.Any(m => m.UserId == currentUserId));
        }

        var groups = await query.OrderBy(g => g.Name).ToListAsync();

        var groupIds = groups.Select(g => g.Id).ToList();
        var datasetsByGroup = await _context.VirtualGroups
            .Where(g => groupIds.Contains(g.Id))
            .SelectMany(g => g.Datasets.Select(d => new { GroupId = g.Id, d.Id, d.Name }))
            .ToListAsync();

        var datasetLookup = datasetsByGroup
            .GroupBy(d => d.GroupId)
            .ToDictionary(g => g.Key, g => g.Select(d => new ControlledDatasetDto { Id = d.Id, Name = d.Name }).OrderBy(d => d.Name).ToList());

        return groups.Select(g => new VirtualGroupDto
        {
            Id = g.Id,
            Name = g.Name,
            Description = g.Description ?? "",
            OwnerId = g.OwnerId,
            OwnerName = g.Owner?.Name ?? "Unknown",
            IsOwner = g.OwnerId == currentUserId,
            CanEdit = g.OwnerId == currentUserId || role == "Admin",
            ControlledDatasets = datasetLookup.TryGetValue(g.Id, out var datasets) ? datasets : new List<ControlledDatasetDto>(),
            Members = g.Members.Select(m => new GroupMemberDto
            {
                UserId = m.UserId,
                Name = m.User.Name,
                Email = m.User.Email
            }).ToList()
        }).ToList();
    }

    public async Task CreateGroupAsync(CreateGroupDto dto)
    {
        var group = new VirtualGroup
        {
            Name = dto.Name,
            Description = dto.Description,
            OwnerId = dto.OwnerId
        };
        _context.VirtualGroups.Add(group);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateGroupAsync(int currentUserId, string roleStr, UpdateGroupDto dto)
    {
        var group = await _context.VirtualGroups.FindAsync(dto.Id);
        if (group == null) return;

        if (roleStr == "Admin")
        {
            group.Name = dto.Name;
            group.Description = dto.Description;
            group.OwnerId = dto.OwnerId;
        }
        else if (group.OwnerId == currentUserId)
        {
            // Non-admin owners can't change the owner
            group.Name = dto.Name;
            group.Description = dto.Description;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<string?> AddGroupMemberAsync(int groupId, int userId)
    {
        if (await _context.VirtualGroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == userId))
        {
            return "User is already a member.";
        }

        _context.VirtualGroupMembers.Add(new VirtualGroupMember { GroupId = groupId, UserId = userId });
        await _context.SaveChangesAsync();
        return null;
    }

    public async Task RemoveGroupMemberAsync(int groupId, int userId)
    {
        var member = await _context.VirtualGroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);
        if (member != null)
        {
            _context.VirtualGroupMembers.Remove(member);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<UserDto>> GetPossibleOwnersAsync()
    {
        return await _context.Users
            .Where(u => u.Role == UserRole.IAO || u.Role == UserRole.Admin)
            .OrderBy(u => u.Name)
            .Select(u => new UserDto { Id = u.Id, Name = u.Name })
            .ToListAsync();
    }

    public async Task<List<UserDto>> GetAllUsersForDropdownAsync()
    {
        return await _context.Users
            .OrderBy(u => u.Name)
            .Select(u => new UserDto { Id = u.Id, Name = u.Name, Email = u.Email })
            .ToListAsync();
    }
}
