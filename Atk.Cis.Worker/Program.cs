using Atk.Cis.Worker;
using Atk.Cis.Service.Interfaces;
using Atk.Cis.Service;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddScoped<ICheckInDeskService, CheckInDeskService>();

var host = builder.Build();
host.Run();
