using System.Collections.Generic;
using System.Threading.Tasks;
using DataProvisioning.Application.DTOs;

namespace DataProvisioning.Application.Interfaces;

public interface IAdministrationService
{
    // Users
    Task<List<UserDto>> GetUsersAsync();
    Task AddUserAsync(string name, string email, string role);
    Task UpdateUserRoleAsync(int userId, string role);
    Task UpdateUserNameAsync(int userId, string name);

    // Groups
    Task<List<VirtualGroupDto>> GetGroupsAsync(int currentUserId, string role);
    Task CreateGroupAsync(CreateGroupDto dto);
    Task UpdateGroupAsync(int currentUserId, string role, UpdateGroupDto dto);
    Task<string?> AddGroupMemberAsync(int groupId, int userId);
    Task RemoveGroupMemberAsync(int groupId, int userId);
    
    // Helpers
    Task<List<UserDto>> GetPossibleOwnersAsync();
    Task<List<UserDto>> GetAllUsersForDropdownAsync();
}
