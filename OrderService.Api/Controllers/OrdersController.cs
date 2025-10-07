using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Api.Data;
using OrderService.Api.IntegrationEvents;
using OrderService.Api.Models;
using Shared.Contracts.Events;

namespace OrderService.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
public class OrdersController(
    OrderDbContext dbContext,
    IOrderIntegrationEventService eventService,
    ILogger<OrdersController> logger) : ControllerBase
{
    public record CreateOrderRequest(Guid UserId, decimal Amount, string Description);

    [HttpPost]
    [ProducesResponseType(typeof(Order), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var newOrder = new Order
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Amount = request.Amount,
            Status = OrderStatus.New,
            Description = request.Description
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            dbContext.Orders.Add(newOrder);
            await dbContext.SaveChangesAsync(cancellationToken);

            var orderCreatedEvent = new OrderCreatedEvent
            {
                OrderId = newOrder.Id,
                Amount = newOrder.Amount,
                UserId = newOrder.UserId
            };

            await eventService.PublishOrderCreatedEventAsync(orderCreatedEvent, cancellationToken);
            
            await transaction.CommitAsync(cancellationToken);
            
            logger.LogInformation("Заказ {OrderId} успешно создан.", newOrder.Id);

            return CreatedAtAction(nameof(GetOrderById), new { id = newOrder.Id }, newOrder);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании заказа для пользователя {UserId}", request.UserId);
            await transaction.RollbackAsync(cancellationToken);
            return StatusCode(StatusCodes.Status500InternalServerError, "Ошибка при обработке запроса.");
        }
    }
    
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.FindAsync([id], cancellationToken: cancellationToken);
        return order == null ? NotFound() : Ok(order);
    }

    public record OrdersResponse(
        IEnumerable<Order> Orders, 
        int Page, 
        int PageSize, 
        int TotalCount);
    
    [HttpGet]
    [ProducesResponseType(typeof(OrdersResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] Guid? userId = null,
        [FromQuery] OrderStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var query = dbContext.Orders.AsQueryable();
            
            if (userId.HasValue)
            {
                query = query.Where(o => o.UserId == userId.Value);
            }
            
            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status.Value);
            }
            
            var totalCount = await query.CountAsync(cancellationToken);
            
            var orders = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return Ok(new OrdersResponse(
                Orders: orders,
                Page: page,
                PageSize: pageSize,
                TotalCount: totalCount
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка заказов");
            return StatusCode(StatusCodes.Status500InternalServerError, "Ошибка при обработке запроса");
        }
    }
}
