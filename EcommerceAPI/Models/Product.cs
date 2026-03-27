using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Models
{
    public class Product : AuditableEntity
    {
        [Key] public Guid Id { get; set; }
        public Guid VendorId { get; set; }
        public int CategoryId { get; set; }
        [Required, StringLength(200)] public string Name { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        [StringLength(50)] public string Status { get; set; } = "Active";
    }
}
