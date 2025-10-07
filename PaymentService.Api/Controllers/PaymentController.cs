using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentService.Api.Data;
using PaymentService.Api.Models;
using Shared.Contracts.Events;

namespace PaymentService.Api.Controllers;

[ApiController]
[Route("api/v1/payments")]
public class OrdersController(
    PaymentDbContext dbContext,
    ILogger<OrdersController> logger) : ControllerBase
{
    public record CreateAccountRequest(Guid UserId);

    [HttpPost]
    [ProducesResponseType(typeof(Account), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var newAccount = new Account
        {
            UserId = request.UserId,
            Balance = 0
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var existingAccount = await dbContext.Accounts.
                FirstOrDefaultAsync(a => a.UserId == newAccount.UserId);
            
            if (existingAccount != null)
            {
                logger.LogInformation("Аккаунт уже существует в системе");
                return CreatedAtAction(nameof(GetBalanceById), new { id = existingAccount.UserId }, existingAccount);
            }
            
            dbContext.Accounts.Add(newAccount);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            logger.LogInformation("Аккаунт {UserId} успешно создан.", newAccount.UserId);

            return CreatedAtAction(nameof(GetBalanceById), new { id = newAccount.UserId }, newAccount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании заказа для пользователя {UserId}", request.UserId);
            await transaction.RollbackAsync(cancellationToken);
            return StatusCode(StatusCodes.Status500InternalServerError, "Ошибка при обработке запроса.");
        }
    }
    
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Account), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBalanceById(Guid id, CancellationToken cancellationToken)
    {
        var account = await dbContext.Accounts.FindAsync([id], cancellationToken: cancellationToken);
        return account == null ? NotFound() : Ok(account);
    }
    
    public record TopUpRequest(decimal Amount);
    [HttpPost("{userId:guid}/top-up")]
    [ProducesResponseType(typeof(Account), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TopUpAccount(
        Guid userId,
        [FromBody] TopUpRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            logger.LogWarning("Попытка пополнения счёта не положительной суммой: {Amount}", request.Amount);
            return BadRequest("Сумма пополнения должна быть больше нуля");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
    
        try
        {
            var account = await dbContext.Accounts
                .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);
        
            if (account == null)
            {
                logger.LogWarning("Аккаунт пользователя {UserId} не найден", userId);
                return NotFound();
            }

            account.Balance += request.Amount;
            dbContext.Accounts.Update(account);
            await dbContext.SaveChangesAsync(cancellationToken);
        
            await transaction.CommitAsync(cancellationToken);
        
            logger.LogInformation(
                "Счёт пользователя {UserId} пополнен на {Amount}. Новый баланс: {Balance}",
                userId, request.Amount, account.Balance);
        
            return Ok(account);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при пополнении счёта пользователя {UserId}", userId);
            await transaction.RollbackAsync(cancellationToken);
            return StatusCode(StatusCodes.Status500InternalServerError, "Ошибка при обработке запроса");
        }
    }
}
