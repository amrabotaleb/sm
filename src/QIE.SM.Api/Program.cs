using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using QIE.SM.Api.Middleware;
using QIE.SM.Application.Abstractions;
using QIE.SM.Application.Configuration;
using QIE.SM.Application.Services;
using QIE.SM.Infrastructure.Kafka;
using QIE.SM.Infrastructure.Notifications;
using QIE.SM.Infrastructure.Repositories;
using QIE.SM.Infrastructure.Routing;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
// JSON logs facilitate ingestion into ELK stacks via standard collectors.
builder.Logging.AddJsonConsole();

builder.Services.AddControllers(options =>
{
    // Require authorization by default for API endpoints.
    var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SmAdmin", policy => policy.RequireRole("sm-admin"));
    options.AddPolicy("SmOperator", policy => policy.RequireRole("sm-operator", "sm-admin"));
});

builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection("Kafka"));
builder.Services.Configure<MongoOptions>(builder.Configuration.GetSection("Mongo"));

builder.Services.AddSingleton<IMessagePublisher, KafkaMessagePublisher>();
builder.Services.AddSingleton<IShardRepository, InMemoryShardRepository>();
builder.Services.AddSingleton<IShardRouter, ConsistentHashShardRouter>();
builder.Services.AddSingleton<IEventNotificationStore, InMemoryEventNotificationStore>();
builder.Services.AddScoped<ShardAdminService>();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<PrincipalFromHeadersMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();
