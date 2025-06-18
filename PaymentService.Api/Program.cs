using MassTransit;
using Microsoft.EntityFrameworkCore;
using PaymentService.Api.Consumers;
using PaymentService.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=payments.db";
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddMassTransit(mt =>
{
    mt.AddConsumer<OrderCreatedConsumer>();
    mt.UsingRabbitMq((context, cfg) =>
    {
        var host = builder.Configuration["RabbitMq:Host"] ?? "localhost";
        cfg.Host(host, "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
            h.RequestedConnectionTimeout(TimeSpan.FromSeconds(30));
        });
        
        cfg.ReceiveEndpoint("order-created-queue", e =>
        {
            e.ConfigureConsumer<OrderCreatedConsumer>(context);
        });
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    db.Database.Migrate();
    app.Logger.LogInformation("Миграции БД PaymentService успешно применены.");
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Ошибка миграции БД PaymentService.");
}

app.Run();