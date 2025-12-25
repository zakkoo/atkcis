using Atk.Cis.Worker;
using Atk.Cis.Service;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddCisServices(builder.Configuration);

var host = builder.Build();
host.Run();
