
namespace AWS.Beanstalk.WebApplication.Models
{
    public class Product
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public override string ToString()
        {
            return "Product Found: " + " Name: " + Name + " Price: " + Price;
        }
    }
}
