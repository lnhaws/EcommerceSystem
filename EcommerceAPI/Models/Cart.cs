using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Models
{
    public class Cart : AuditableEntity
    {
        [Key] public Guid Id { get; set; }

        // Mỗi giỏ hàng thuộc về 1 User duy nhất
        public Guid UserId { get; set; }

        // Liên kết 1-Nhiều với các món hàng bên trong
        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }
}