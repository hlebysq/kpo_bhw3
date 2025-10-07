using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderService.Api.Models;

public enum OrderStatus
{
    New,
    Finished,
    Cancelled
}

public class Order
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }

    [Required]
    public OrderStatus Status { get; set; }
    [Required]
    public string Description { get; set; }
}
