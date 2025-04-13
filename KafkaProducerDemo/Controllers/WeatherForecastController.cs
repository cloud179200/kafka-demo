using System.Threading.Tasks;
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

        [HttpGet("compared-data")]
        public async Task<IActionResult> GetComparedData()
        {
            var sourceData1 = await _orderPaymentService.GetListOrderPaymentPostgreSql();
            var sourceData2 = await _orderPaymentService.GetListOrderPaymentPostgreSql2();
            var syncDataPostgreSql1 = await _orderPaymentService.GetListOrderPaymentPostgreSqlCompared();
            var syncDataPostgreSql2 = await _orderPaymentService.GetListOrderPaymentPostgreSqlCompared2();
            var syncDataMySql1 = await _orderPaymentService.GetListOrderPaymentMySqlCompared();
            var syncDataMySql2 = await _orderPaymentService.GetListOrderPaymentMySqlCompared2();
            //calculate the difference percent between the source data and the sync data
            var sourceData = sourceData1.Concat(sourceData2).ToList();
            var percentPostgreSql1 = _orderPaymentService.CalculateDifferencePercentage(sourceData, syncDataPostgreSql1);
            var percentPostgreSql2 = _orderPaymentService.CalculateDifferencePercentage(sourceData, syncDataPostgreSql2);
            var percentMySql1 = _orderPaymentService.CalculateDifferencePercentage(sourceData, syncDataMySql1);
            var percentMySql2 = _orderPaymentService.CalculateDifferencePercentage(sourceData, syncDataMySql2);
            var response = new
            {
                PercentSource = 100,
                PercentPostgreSql1 = percentPostgreSql1.percent,
                ListRecordNotSyncPostgreSql1 = percentPostgreSql1.listRecordNotFound,
                PercentPostgreSql2 = percentPostgreSql2.percent,
                ListRecordNotSyncPostgreSql2 = percentPostgreSql2.listRecordNotFound,
                PercentMySql1 = percentMySql1.percent,
                ListRecordNotSyncMySql1 = percentMySql1.listRecordNotFound,
                PercentMySql2 = percentMySql2.percent,
                ListRecordNotSyncMySql2 = percentMySql2.listRecordNotFound
            };

            return Ok(response);
        }
    }
}