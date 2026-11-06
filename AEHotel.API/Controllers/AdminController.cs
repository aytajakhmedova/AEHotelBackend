using AEHotel.API.Security;
using AEHotel.Service.DTOs.Admin;
using AEHotel.Service.Security;
using AEHotel.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEHotel.API.Controllers;

// Customers, staff and reports for the admin panel. Everything here is limited to SuperAdmin and Admin.
[ApiController]
[Authorize(Roles = AppRoles.Staff)]
[Route("api")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _admin;

    public AdminController(IAdminService admin)
    {
        _admin = admin;
    }

    [HttpGet("customers")]
    public async Task<ActionResult<IReadOnlyList<CustomerResponse>>> GetCustomers(CancellationToken cancellationToken)
    {
        return Ok(await _admin.GetCustomersAsync(cancellationToken));
    }

    [HttpGet("customers/{id}/bookings")]
    public async Task<ActionResult<IReadOnlyList<CustomerBookingResponse>>> GetCustomerBookings(string id, CancellationToken cancellationToken)
    {
        return Ok(await _admin.GetCustomerBookingsAsync(id, cancellationToken));
    }

    [HttpGet("staff")]
    public async Task<ActionResult<IReadOnlyList<StaffResponse>>> GetStaff(CancellationToken cancellationToken)
    {
        return Ok(await _admin.GetStaffAsync(cancellationToken));
    }

    [HttpPost("staff")]
    public async Task<ActionResult<StaffResponse>> CreateStaff(StaffRequest request, CancellationToken cancellationToken)
    {
        return StatusCode(StatusCodes.Status201Created, await _admin.CreateStaffAsync(request, User.GetRequiredUserId(), cancellationToken));
    }

    [HttpPut("staff/{id}")]
    public async Task<ActionResult<StaffResponse>> UpdateStaff(string id, StaffRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _admin.UpdateStaffAsync(id, request, User.GetRequiredUserId(), cancellationToken));
    }

    [HttpDelete("staff/{id}")]
    public async Task<IActionResult> DeleteStaff(string id, CancellationToken cancellationToken)
    {
        await _admin.DeleteStaffAsync(id, User.GetRequiredUserId(), cancellationToken);
        return NoContent();
    }

    [HttpGet("reports")]
    public async Task<ActionResult<ReportResponse>> GetReport([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    {
        return Ok(await _admin.GetReportAsync(from, to, cancellationToken));
    }
}
