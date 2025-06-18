using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentService.Api.Models;

namespace PaymentService.Api.Data;

public class PaymentDbContext : DbContext
{
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }
    public class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.ToTable("Accounts");
            builder.HasKey(a => a.UserId);
            builder.Property(a => a.Balance).HasColumnType("decimal(18,2)");
        }
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>()
            .Property(p => p.Status)
            .HasConversion<string>();
        modelBuilder.ApplyConfiguration(new AccountConfiguration());

    }
}
