using AEHotel.Service.DTOs.Payments;
using AEHotel.Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AEHotel.API.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PaymentResponse>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _paymentService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PaymentResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        return Ok(await _paymentService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PaymentResponse>> UpdateStatus(int id, PaymentStatusRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _paymentService.UpdateStatusAsync(id, request.Status, cancellationToken));
    }
}
