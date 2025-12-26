using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QIE.SM.Application.Abstractions;
using QIE.SM.Application.Configuration;
using QIE.SM.Infrastructure.Kafka;
using QIE.SM.Infrastructure.Notifications;
using QIE.SM.Infrastructure.Provisioning;
using QIE.SM.Infrastructure.Repositories;
using QIE.SM.Infrastructure.Routing;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
// JSON logs facilitate ingestion into ELK stacks via standard collectors.
builder.Logging.AddJsonConsole();

builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection("Kafka"));
builder.Services.Configure<WorkerGroupOptions>(builder.Configuration.GetSection("Kafka"));
builder.Services.Configure<MongoOptions>(builder.Configuration.GetSection("Mongo"));
builder.Services.Configure<EventNotificationFilterOptions>(builder.Configuration.GetSection("EventNotificationFilter"));
builder.Services.Configure<ShardProvisionerOptions>(builder.Configuration.GetSection("ShardProvisioner"));

builder.Services.AddSingleton<IMessagePublisher, KafkaMessagePublisher>();
builder.Services.AddSingleton<IKafkaConsumerFactory, KafkaConsumerFactory>();
builder.Services.AddSingleton<IEnrollmentManifestRepository, MongoEnrollmentManifestRepository>();
builder.Services.AddSingleton<IShardRepository, InMemoryShardRepository>();
builder.Services.AddSingleton<IShardRouter, ConsistentHashShardRouter>();
builder.Services.AddSingleton<IEventNotificationStore, InMemoryEventNotificationStore>();

builder.Services.AddSingleton<KubernetesShardProvisioner>();
builder.Services.AddSingleton<AgentGrpcShardProvisioner>();
builder.Services.AddSingleton<IShardProvisioner, ShardProvisionerSelector>();

builder.Services.AddHostedService<EnrollmentIngestWorker>();
builder.Services.AddHostedService<ShardManagementWorker>();
builder.Services.AddHostedService<EventNotificationWorker>();

var host = builder.Build();

await host.RunAsync();
