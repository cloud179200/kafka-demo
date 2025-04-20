using KafkaProducerDemo.Services;
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

// Register Kafka producer service
builder.Services.AddSingleton<KafkaProducerService>();
builder.Services.AddSingleton<OrderPaymentService>();
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

// Configure CORS
builder.Services.AddCors(options =>
{
  options.AddPolicy("AllowSpecificOrigins", policy =>
  {
    policy.WithOrigins("http://localhost:3000") // Allow requests from localhost:3000
            .AllowAnyHeader() // Allow any headers
            .AllowAnyMethod(); // Allow any HTTP methods (GET, POST, etc.)
  });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowSpecificOrigins");

app.UseAuthorization();

app.MapControllers();

app.Run();
