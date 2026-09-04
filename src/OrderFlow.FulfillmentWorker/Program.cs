using OrderFlow.FulfillmentWorker;
using OrderFlow.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<FulfillmentConsumer>();

var host = builder.Build();
host.Run();
