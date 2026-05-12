using System;
using System.Collections.Generic;

namespace DataProvisioning.Application.DTOs;

public class DatasetDetailsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? GroupName { get; set; }
    public int? OwnerGroupId { get; set; }
    
    public List<DatasetColumnDto> Columns { get; set; } = new();
    public List<DatasetRequestDto> UserRequests { get; set; } = new();
    public List<AssetPolicyGroupDto> AvailablePolicies { get; set; } = new();
    public List<DatasetReportDto> LinkedReports { get; set; } = new();
    public List<DatasetApproverDto> OwnerGroupMembers { get; set; } = new();
    
    // Authorization flags
    public bool HasFullAccess { get; set; }
    public bool IsOwnerOrAdmin { get; set; }
}

public class DatasetColumnDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DataType { get; set; }
    public string? Definition { get; set; }
    public bool IsPii { get; set; }
    public string? SampleData { get; set; }
}

public class DatasetRequestDto
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PolicyName { get; set; } = "Full Dataset";
    public int? PolicyGroupId { get; set; }
    public string? ReviewerName { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public List<DatasetApproverDto> PendingApprovers { get; set; } = new();
    public List<DatasetApproverDto> GlobalApprovers { get; set; } = new();
}

public class DatasetApproverDto
{
    public string Name { get; set; } = string.Empty;
    public string RoleType { get; set; } = string.Empty; // "Owner", "Member", "Admin", etc.
}

public class AssetPolicyGroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class DatasetReportDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Url { get; set; }
}

public class EditDatasetDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? OwnerGroupId { get; set; }
    
    public List<DataProvisioning.Application.DTOs.VirtualGroupDto> AvailableGroups { get; set; } = new();
}

