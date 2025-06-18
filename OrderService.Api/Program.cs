using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Api.Consumers;
using OrderService.Api.Data;
using OrderService.Api.Hubs;
using OrderService.Api.IntegrationEvents;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=orders.db";
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<IOrderIntegrationEventService, OrderIntegrationEventService>();
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:8080", "null")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddMassTransit(mt =>
{
    mt.AddConsumer<PaymentProcessedConsumer>();
    mt.UsingRabbitMq((context, cfg) =>
    {
        var host = builder.Configuration["RabbitMq:Host"] ?? "localhost";
        cfg.Host(host, "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
            h.RequestedConnectionTimeout(TimeSpan.FromSeconds(30));

        });
        
        cfg.ReceiveEndpoint("payment-processed-queue", e =>
        {
            e.ConfigureConsumer<PaymentProcessedConsumer>(context);
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
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthorization();

app.MapControllers();
app.MapHub<OrderHub>("/orderHub");

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    db.Database.Migrate();
    app.Logger.LogInformation("Миграции БД OrderService успешно применены.");
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Ошибка миграции БД OrderService.");
}

app.Run();
