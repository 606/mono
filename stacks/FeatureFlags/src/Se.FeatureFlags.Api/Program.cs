using Se.FeatureFlags.Api.Abstractions;
using Se.FeatureFlags.Api.Endpoints;
using Se.FeatureFlags.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IFlagRepository>(_ => new JsonFlagRepository("flags.json"));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

// Map endpoints
app.MapHealthEndpoints();
app.MapFlagEndpoints();

app.Run();
