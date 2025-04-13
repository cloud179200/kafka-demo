using KafkaProducerDemo.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Kafka producer service
builder.Services.AddSingleton<KafkaProducerService>();
builder.Services.AddSingleton<OrderPaymentService>();
builder.Services.AddSingleton<PostgreSqlService>();
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.MapControllers();

app.Run();
