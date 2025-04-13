using KafkaProducerDemo.Models;

namespace KafkaProducerDemo.StandardizeModels
{
  public class StandardizeUserModel
  {
    public StandardizeUserModel(User user)
    {
      Id = user.Id;
      Name = user.Name;
      Email = user.Email;
      CreatedAt = user.CreatedAt;
    }
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
  }
}