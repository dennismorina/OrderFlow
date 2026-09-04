using System.Text.Json.Serialization;
using OrderFlow.Api.Errors;
using OrderFlow.Api.HostedServices;
using OrderFlow.Api.Startup;
using OrderFlow.Application;
using OrderFlow.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()));

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddHealthChecks();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<OutboxPublisherService>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (app.Configuration.GetValue("Database:AutoMigrate", true))
{
    await DatabaseInitializer.InitializeAsync(
        app.Services,
        app.Lifetime.ApplicationStopping);
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
