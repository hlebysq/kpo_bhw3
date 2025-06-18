using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OrderService.Api.Data;
using OrderService.Api.Hubs;
using OrderService.Api.Models;
using Shared.Contracts.Events;

namespace OrderService.Api.Consumers;

public class PaymentProcessedConsumer(
    OrderDbContext dbContext,
    IHubContext<OrderHub> hubContext,
    ILogger<PaymentProcessedConsumer> logger)
    : IConsumer<PaymentProcessedEvent>
{
    public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        var message = context.Message;
        logger.LogInformation(
            "Получено событие PaymentProcessedEvent для заказа {OrderId}. Результат: {Status}",
            message.OrderId,
            message.IsSuccess ? "Успех" : "Неудача");

        var order = await dbContext.Orders.FindAsync(message.OrderId);

        if (order == null)
        {
            logger.LogWarning("Заказ с Id {OrderId} не найден.", message.OrderId);
            return;
        }

        order.Status = message.IsSuccess ? OrderStatus.Finished : OrderStatus.Cancelled;
        dbContext.Entry(order).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(context.CancellationToken);
        logger.LogInformation("Статус заказа {OrderId} обновлен на {NewStatus}", order.Id, order.Status);

        await hubContext.Clients.All.SendAsync(
            "ReceiveOrderStatusUpdate",
            new { orderId = order.Id, status = order.Status.ToString() },
            cancellationToken: context.CancellationToken);
        logger.LogInformation("Уведомление о статусе заказа {OrderId} отправлено через SignalR.", order.Id);
    }
}
