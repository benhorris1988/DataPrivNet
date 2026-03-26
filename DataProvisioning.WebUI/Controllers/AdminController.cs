using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataProvisioning.Application.Interfaces;
using DataProvisioning.Application.DTOs;
using DataProvisioning.WebUI.Models;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DataProvisioning.WebUI.Controllers;

[Authorize(Roles = "Admin,IAO,IAA")]
public class AdminController : Controller
{
    private readonly IAdministrationService _adminService;
    private readonly IConfiguration _configuration;
    private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;

    public AdminController(IAdministrationService adminService, IConfiguration configuration, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
    {
        _adminService = adminService;
        _configuration = configuration;
        _env = env;
    }

    public IActionResult Index()
    {
        if (User.IsInRole("Admin"))
            return RedirectToAction(nameof(Users));
            
        return RedirectToAction(nameof(Groups));
    }

    // --- Users ---
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Users()
    {
        var users = await _adminService.GetUsersAsync();
        return View(users);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddUser(string name, string email, string role)
    {
        await _adminService.AddUserAsync(name, email, role);
        TempData["SuccessMessage"] = "User added successfully.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUserRole(int userId, string role)
    {
        await _adminService.UpdateUserRoleAsync(userId, role);
        TempData["SuccessMessage"] = "User role updated successfully.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUserName(int userId, string name)
    {
        await _adminService.UpdateUserNameAsync(userId, name);
        TempData["SuccessMessage"] = "User name updated successfully.";
        return RedirectToAction(nameof(Users));
    }

    // --- Groups ---
    public async Task<IActionResult> Groups()
    {
        int currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        string role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "User";

        var groups = await _adminService.GetGroupsAsync(currentUserId, role);
        ViewBag.PossibleOwners = await _adminService.GetPossibleOwnersAsync();
        ViewBag.AllUsers = await _adminService.GetAllUsersForDropdownAsync();

        return View(groups);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateGroup(CreateGroupDto dto)
    {
        int currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        // If not admin, force owner to self
        if (!User.IsInRole("Admin"))
        {
            dto.OwnerId = currentUserId;
        }

        await _adminService.CreateGroupAsync(dto);
        TempData["SuccessMessage"] = "Group created successfully.";
        return RedirectToAction(nameof(Groups));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateGroup(UpdateGroupDto dto)
    {
        int currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        string role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "User";

        await _adminService.UpdateGroupAsync(currentUserId, role, dto);
        TempData["SuccessMessage"] = "Group updated successfully.";
        return RedirectToAction(nameof(Groups));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddGroupMember(int groupId, int userId)
    {
        var error = await _adminService.AddGroupMemberAsync(groupId, userId);
        if (error != null)
            TempData["ErrorMessage"] = error;
        else
            TempData["SuccessMessage"] = "User added successfully.";

        return RedirectToAction(nameof(Groups));
    }

    // --- Configuration (Admin Centre) ---
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult Config()
    {
        var vm = new AdminConfigViewModel();
        vm.ExecutingAccount = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
        
        // Parse DB
        var defConn = _configuration.GetConnectionString("DefaultConnection") ?? "";
        vm.DbHost = ExtractConnStringValue(defConn, "Server");
        vm.DbName = ExtractConnStringValue(defConn, "Database");
        vm.DbUser = ExtractConnStringValue(defConn, "User Id");
        vm.DbUseWindowsAuth = defConn.Contains("Trusted_Connection=True", System.StringComparison.OrdinalIgnoreCase) || defConn.Contains("Integrated Security=True", System.StringComparison.OrdinalIgnoreCase);

        // Parse DW
        var dwConn = _configuration.GetConnectionString("DataWarehouseConnection") ?? "";
        vm.DwHost = ExtractConnStringValue(dwConn, "Server");
        vm.DwName = ExtractConnStringValue(dwConn, "Database");
        vm.DwUser = ExtractConnStringValue(dwConn, "User Id");
        vm.DwUseWindowsAuth = dwConn.Contains("Trusted_Connection=True", System.StringComparison.OrdinalIgnoreCase) || dwConn.Contains("Integrated Security=True", System.StringComparison.OrdinalIgnoreCase);

        // AD
        vm.AdEnabled = _configuration.GetValue<bool>("ActiveDirectory:Enabled");
        vm.AdDomain = _configuration["ActiveDirectory:Domain"] ?? "";
        vm.AdServer = _configuration["ActiveDirectory:Server"] ?? "";
        vm.AdBaseDn = _configuration["ActiveDirectory:BaseDn"] ?? "";

        // Entra
        vm.EntraEnabled = _configuration.GetValue<bool>("AzureAd:Enabled");
        vm.EntraTenantId = _configuration["AzureAd:TenantId"] ?? "";
        vm.EntraClientId = _configuration["AzureAd:ClientId"] ?? "";
        
        return View(vm);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Config(AdminConfigViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        string appSettingsPath = Path.Combine(_env.ContentRootPath, "appsettings.json");
        string json = await System.IO.File.ReadAllTextAsync(appSettingsPath);
        var jsonNode = JsonNode.Parse(json);

        if (jsonNode != null)
        {
            // Build Conn Strings
            string defaultConn = $"Server={model.DbHost};Database={model.DbName};TrustServerCertificate=True;";
            if (model.DbUseWindowsAuth)
            {
                defaultConn += "Trusted_Connection=True;";
            }
            else if (!string.IsNullOrEmpty(model.DbUser))
            {
                 // Keep old pass if new is empty
                 string pass = string.IsNullOrEmpty(model.DbPass) ? ExtractConnStringValue(_configuration.GetConnectionString("DefaultConnection")??"", "Password") : model.DbPass;
                 defaultConn += $"User Id={model.DbUser};Password={pass};";
            }

            string dwConn = $"Server={model.DwHost};Database={model.DwName};TrustServerCertificate=True;";
            if (model.DwUseWindowsAuth)
            {
                dwConn += "Trusted_Connection=True;";
            }
            else if (!string.IsNullOrEmpty(model.DwUser))
            {
                 string pass = string.IsNullOrEmpty(model.DwPass) ? ExtractConnStringValue(_configuration.GetConnectionString("DataWarehouseConnection")??"", "Password") : model.DwPass;
                 dwConn += $"User Id={model.DwUser};Password={pass};";
            }

            if (jsonNode["ConnectionStrings"] == null) jsonNode["ConnectionStrings"] = new JsonObject();
            jsonNode["ConnectionStrings"]["DefaultConnection"] = defaultConn;
            jsonNode["ConnectionStrings"]["DataWarehouseConnection"] = dwConn;

            // Azure AD
            if (jsonNode["AzureAd"] == null) jsonNode["AzureAd"] = new JsonObject();
            jsonNode["AzureAd"]["Enabled"] = model.EntraEnabled;
            jsonNode["AzureAd"]["TenantId"] = model.EntraTenantId;
            jsonNode["AzureAd"]["ClientId"] = model.EntraClientId;
            if (!string.IsNullOrEmpty(model.EntraClientSecret))
                jsonNode["AzureAd"]["ClientSecret"] = model.EntraClientSecret;

            // AD
            if (jsonNode["ActiveDirectory"] == null) jsonNode["ActiveDirectory"] = new JsonObject();
            jsonNode["ActiveDirectory"]["Enabled"] = model.AdEnabled;
            jsonNode["ActiveDirectory"]["Domain"] = model.AdDomain;
            jsonNode["ActiveDirectory"]["Server"] = model.AdServer;
            jsonNode["ActiveDirectory"]["BaseDn"] = model.AdBaseDn;

            var options = new JsonSerializerOptions { WriteIndented = true };
            await System.IO.File.WriteAllTextAsync(appSettingsPath, jsonNode.ToJsonString(options));
        }

        TempData["SuccessMessage"] = "Settings saved successfully! You may need to restart the application for connection string changes to fully apply.";
        return RedirectToAction(nameof(Config));
    }

    private string ExtractConnStringValue(string connString, string key)
    {
        if (string.IsNullOrEmpty(connString)) return "";
        var parts = connString.Split(';');
        foreach(var part in parts)
        {
            var kvp = part.Split('=', 2);
            if (kvp.Length == 2 && kvp[0].Trim().Equals(key, System.StringComparison.OrdinalIgnoreCase))
            {
                return kvp[1].Trim();
            }
        }
        return "";
    }
}
