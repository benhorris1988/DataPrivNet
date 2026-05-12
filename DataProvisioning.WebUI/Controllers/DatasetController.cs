using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataProvisioning.Application.Interfaces;
using DataProvisioning.Application.DTOs;
using DataProvisioning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DataProvisioning.WebUI.Controllers;

[Authorize]
public class DatasetController : Controller
{
    private readonly IDatasetDetailsService _datasetService;
    private readonly IAccessRequestService _requestService;
    private readonly IApplicationDbContext _context;

    public DatasetController(IDatasetDetailsService datasetService, IAccessRequestService requestService, IApplicationDbContext context)
    {
        _datasetService = datasetService;
        _requestService = requestService;
        _context = context;
    }

    [HttpGet("Dataset/Details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        // Extract authenticated user ID from Claims
        int currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "User";

        var dto = await _datasetService.GetDatasetDetailsAsync(id, currentUserId, userRole);

        if (dto == null)
        {
            return NotFound("Dataset not found");
        }

        ViewBag.CurrentUserId = currentUserId;
        ViewBag.UserRole = userRole;

        return View(dto);
    }

    [HttpPost("Dataset/RequestAccess")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestAccess(int id, string justification, int? policy_group_id)
    {
        int currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

        var dto = new SubmitRequestDto
        {
            DatasetId = id,
            Justification = justification,
            PolicyGroupId = policy_group_id == 0 ? null : policy_group_id
        };

        await _requestService.SubmitRequestAsync(currentUserId, dto);
        
        TempData["SuccessMessage"] = "Request submitted successfully.";
        return RedirectToAction("Details", new { id = id });
    }

    [HttpPost("Dataset/CancelRequest")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelRequest(int requestId, int datasetId)
    {
        int currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

        await _requestService.CancelOrRemoveRequestAsync(requestId, currentUserId);
        
        TempData["SuccessMessage"] = "Access request cancelled.";
        return RedirectToAction("Details", new { id = datasetId });
    }

    [HttpGet("Dataset/Edit/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        int currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "User";

        var dto = await _datasetService.GetEditDatasetAsync(id, currentUserId, userRole);
        if (dto == null)
            return Forbid();

        return View(dto);
    }

    [HttpPost("Dataset/Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditDatasetDto dto)
    {
        if (id != dto.Id) return BadRequest();
        
        int currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "User";
        
        // RE-AUTHORIZE
        var authCheck = await _datasetService.GetEditDatasetAsync(id, currentUserId, userRole);
        if (authCheck == null) return Forbid();

        if (ModelState.IsValid)
        {
            await _datasetService.UpdateDatasetAsync(dto);
            TempData["SuccessMessage"] = "Dataset updated successfully.";
            return RedirectToAction("Details", new { id = id });
        }

        dto.AvailableGroups = authCheck.AvailableGroups; // Repopulate for dropdown
        return View(dto);
    }

    [HttpPost("Dataset/AddReport")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddReport(int datasetId, string reportName, string reportUrl)
    {
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "User";
        if (userRole != "Admin" && userRole != "IAO")
            return Forbid();

        await _datasetService.AddReportAsync(datasetId, reportName, reportUrl);
        TempData["SuccessMessage"] = "Report linked successfully.";
        
        return RedirectToAction("Details", new { id = datasetId });
    }
}
