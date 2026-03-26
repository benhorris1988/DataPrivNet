using System.ComponentModel.DataAnnotations;

namespace DataProvisioning.WebUI.Models;

public class AdminConfigViewModel
{
    public string ExecutingAccount { get; set; } = string.Empty;

    // App Database
    [Required]
    [Display(Name = "Server Host")]
    public string DbHost { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Database Name")]
    public string DbName { get; set; } = string.Empty;

    [Display(Name = "Use Windows Authentication")]
    public bool DbUseWindowsAuth { get; set; }

    [Display(Name = "Username")]
    public string? DbUser { get; set; }

    [Display(Name = "Password")]
    public string? DbPass { get; set; }

    // Data Warehouse (Scanning)
    [Required]
    [Display(Name = "Server Host")]
    public string DwHost { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Database Name")]
    public string DwName { get; set; } = string.Empty;

    [Display(Name = "Use Windows Authentication")]
    public bool DwUseWindowsAuth { get; set; }

    [Display(Name = "Username")]
    public string? DwUser { get; set; }

    [Display(Name = "Password")]
    public string? DwPass { get; set; }

    // AD / LDAP Settings
    [Display(Name = "Enable AD")]
    public bool AdEnabled { get; set; }

    [Display(Name = "Domain")]
    public string AdDomain { get; set; } = string.Empty;

    [Display(Name = "Server URL")]
    public string AdServer { get; set; } = string.Empty;

    [Display(Name = "Base DN")]
    public string AdBaseDn { get; set; } = string.Empty;

    // Entra ID Settings
    [Display(Name = "Enable Entra ID")]
    public bool EntraEnabled { get; set; }

    [Display(Name = "Tenant ID")]
    public string EntraTenantId { get; set; } = string.Empty;

    [Display(Name = "Client ID")]
    public string EntraClientId { get; set; } = string.Empty;

    [Display(Name = "Client Secret")]
    public string? EntraClientSecret { get; set; }
}
