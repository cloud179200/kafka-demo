namespace KafkaConsumerDemo.StandardizeModels
{
  public class OrderPaymentRecord
  {
    public long OrderId { get; set; }
    public int CustomerId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public required string OrderState { get; set; }
    public long? PaymentId { get; set; }
    public DateTime? PaymentDate { get; set; }
    public decimal? PaymentAmount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentState { get; set; }
  }
}