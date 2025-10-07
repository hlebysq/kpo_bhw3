using MassTransit;
using Microsoft.EntityFrameworkCore;
using PaymentService.Api.Data;
using PaymentService.Api.Models;
using Shared.Contracts.Events;

namespace PaymentService.Api.Consumers;

public class OrderCreatedConsumer(
    PaymentDbContext dbContext,
    IPublishEndpoint publishEndpoint,
    ILogger<OrderCreatedConsumer> logger) : IConsumer<OrderCreatedEvent>
{
    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;
        logger.LogInformation("Получено событие OrderCreatedEvent для заказа {OrderId}", message.OrderId);
        
        var userAccount = await dbContext.Accounts.
            FirstOrDefaultAsync(a => a.UserId == message.UserId);
        bool isPaymentSuccessful;
        string? failureReason;
        if (userAccount == null)
        {
            isPaymentSuccessful = false;
            failureReason = "Аккаунта не существует";
        }
        else if (userAccount.Balance - message.Amount < 0)
        {
            isPaymentSuccessful = false;
            failureReason = "Недостаточно средств на счете";
        }
        else
        {
            await using var accTransaction = await dbContext.Database.
                BeginTransactionAsync(context.CancellationToken);
            userAccount.Balance -= message.Amount;
            dbContext.Accounts.Update(userAccount);
            await dbContext.SaveChangesAsync(context.CancellationToken);
        
            await accTransaction.CommitAsync(context.CancellationToken);
            isPaymentSuccessful = true;
            failureReason = null;
        }
        
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = message.OrderId,
            UserId = message.UserId,
            Amount = message.Amount,
            Status = isPaymentSuccessful ? PaymentStatus.Finished : PaymentStatus.Cancelled,
            FailureReason = failureReason
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(context.CancellationToken);
        try
        {
            dbContext.Payments.Add(payment);
            await dbContext.SaveChangesAsync(context.CancellationToken);
            
            var paymentEvent = new PaymentProcessedEvent
            {
                OrderId = message.OrderId,
                IsSuccess = isPaymentSuccessful,
                FailureReason = failureReason
            };

            await publishEndpoint.Publish(paymentEvent, context.CancellationToken);
            await transaction.CommitAsync(context.CancellationToken);
            
            logger.LogInformation("Обработка платежа для заказа {OrderId} завершена: {Status}", 
                message.OrderId, payment.Status);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(context.CancellationToken);
            logger.LogError(ex, "Ошибка обработки платежа для заказа {OrderId}", message.OrderId);
            throw;
        }
    }
}
