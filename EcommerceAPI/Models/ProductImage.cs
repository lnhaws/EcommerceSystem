using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Models
{
    public class ProductImage : AuditableEntity
    {
        [Key] public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        [Required] public string ImageUrl { get; set; } = string.Empty;
        public bool IsMain { get; set; }
        public int DisplayOrder { get; set; }
    }
}
