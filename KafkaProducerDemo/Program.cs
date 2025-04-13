using KafkaProducerDemo.Services;

var builder = WebApplication.CreateBuilder(args);

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
