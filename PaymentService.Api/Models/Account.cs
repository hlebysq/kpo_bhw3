using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaymentService.Api.Models;

public class Account
{
    [Key]
    public Guid UserId { get; set; }
    [Required, Column(TypeName = "decimal(18, 2)")]
    [ConcurrencyCheck]
    public decimal Balance { get; set; }
}