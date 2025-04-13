using KafkaProducerDemo.Services;
using Microsoft.AspNetCore.Mvc;

namespace KafkaProducerDemo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly KafkaProducerService _kafkaProducerService;
        private readonly OrderPaymentService _orderPaymentService;

        public WeatherForecastController(KafkaProducerService kafkaProducerService, OrderPaymentService orderPaymentService)
        {
            _kafkaProducerService = kafkaProducerService;
            _orderPaymentService = orderPaymentService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] string message)
        {
            var key = Guid.NewGuid().ToString(); // Generate a unique key for the message
            await _kafkaProducerService.SendToKafkaAsync("demo", key, message);
            return Ok("Message sent to Kafka.");
        }

        [HttpGet("send-order-payment-data")]
        public async Task<IActionResult> SendOrderPaymentData()
        {
            await _orderPaymentService.ProcessAndSendDataAsync();
            return Ok("Order payment data sent to Kafka.");
        }

    }
}