using Atk.Cis.Worker;
using Atk.Cis.Service.Interfaces;
using Atk.Cis.Service;
using Atk.Cis.Service.Data;

using Microsoft.EntityFrameworkCore;
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddScoped<ICheckInDeskService, CheckInDeskService>();


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var host = builder.Build();
host.Run();
