using KafkaConsumerDemo.Services;
using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.Network;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
{
  var logstashHost = context.Configuration["Logging:Logstash:Host"];
  var logstashPort = context.Configuration["Logging:Logstash:Port"];
  var logstashUri = $"tcp://{logstashHost}:{logstashPort}";

  config.Enrich.FromLogContext()
       .WriteTo.Console()
       .WriteTo.TCPSink(logstashUri, new JsonFormatter());
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register MongoDB and Kafka consumer services
// builder.Services.AddHostedService<KafkaConsumerService>();
builder.Services.AddHostedService<KafkaOrderPaymentConsumerService>();
builder.Services.AddTransient<PostgresSqlService>();
builder.Services.AddTransient<MySqlService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.MapControllers();

app.Run();