using KafkaProducerDemo.Services;
using Microsoft.AspNetCore.Mvc;

namespace KafkaProducerDemo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly KafkaProducerService _kafkaProducerService;

        public WeatherForecastController(KafkaProducerService kafkaProducerService)
        {
            _kafkaProducerService = kafkaProducerService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] string message)
        {
            var key = Guid.NewGuid().ToString(); // Generate a unique key for the message
            await _kafkaProducerService.SendMessageAsync(key, message);
            return Ok("Message sent to Kafka.");
        }
    }
}