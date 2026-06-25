using Microsoft.EntityFrameworkCore;
using RocketLeagueAnalyzer.Collector;
using RocketLeagueAnalyzer.Infrastructure.RlStats;
using RocketLeagueAnalyzer.Persistance.Data;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"))
);

builder.Services.AddRocketLeagueStatsApi(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
