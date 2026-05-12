using System;
using System.Collections.Generic;

namespace DataProvisioning.Application.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class VirtualGroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public bool IsOwner { get; set; }
    public bool CanEdit { get; set; }
    
    public List<ControlledDatasetDto> ControlledDatasets { get; set; } = new();
    public List<GroupMemberDto> Members { get; set; } = new();
}

public class ControlledDatasetDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class GroupMemberDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class CreateGroupDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int OwnerId { get; set; }
}

public class UpdateGroupDto : CreateGroupDto
{
    public int Id { get; set; }
}

public class AdminDashboardDto
{
    public List<UserDto> Users { get; set; } = new();
    public List<VirtualGroupDto> Groups { get; set; } = new();
}
