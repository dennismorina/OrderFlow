using Microsoft.AspNetCore.Mvc;
using OrderFlow.Application.Orders;
using OrderFlow.Application.Orders.Models;

namespace OrderFlow.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;

    public OrdersController(OrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    [ProducesResponseType<OrderDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderDto>> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _orderService.CreateAsync(
            request,
            cancellationToken);

        if (result.IsIdempotentReplay)
        {
            return Ok(result.Order);
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Order.Id },
            result.Order);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<OrderDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> List(
        CancellationToken cancellationToken)
        => Ok(await _orderService.ListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await _orderService.GetAsync(id, cancellationToken));

    [HttpGet("{id:guid}/history")]
    [ProducesResponseType<IReadOnlyList<OrderStatusHistoryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrderStatusHistoryDto>>> History(
        Guid id,
        CancellationToken cancellationToken)
    {
        var order = await _orderService.GetAsync(id, cancellationToken);
        return Ok(order.History);
    }

    [HttpPost("{id:guid}/validate")]
    public async Task<ActionResult<OrderDto>> Validate(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await _orderService.ValidateAsync(id, cancellationToken));

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<OrderDto>> Approve(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await _orderService.ApproveAsync(id, cancellationToken));

    [HttpPost("{id:guid}/start-processing")]
    public async Task<ActionResult<OrderDto>> StartProcessing(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await _orderService.StartProcessingAsync(id, cancellationToken));

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<OrderDto>> Complete(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await _orderService.CompleteAsync(id, cancellationToken));

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<OrderDto>> Cancel(
        Guid id,
        [FromBody] CancelOrderRequest request,
        CancellationToken cancellationToken)
        => Ok(await _orderService.CancelAsync(
            id,
            request.Reason,
            cancellationToken));
}
