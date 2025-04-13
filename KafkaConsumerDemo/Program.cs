using KafkaConsumerDemo.Services;

var builder = WebApplication.CreateBuilder(args);

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