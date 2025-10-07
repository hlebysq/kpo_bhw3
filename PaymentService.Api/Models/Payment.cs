using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaymentService.Api.Models;

public enum PaymentStatus { Finished, Cancelled }

public class Payment
{
    [Key]
    public Guid Id { get; set; }
    [Required]
    public Guid OrderId { get; set; }
    [Required]
    public Guid UserId { get; set; }
    [Required, Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }
    [Required]
    public PaymentStatus Status { get; set; }
    public string? FailureReason { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
