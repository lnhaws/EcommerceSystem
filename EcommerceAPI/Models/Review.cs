using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Models
{
    // --- REVIEW ---
    public class Review : AuditableEntity
    {
        [Key] public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public Guid VariantId { get; set; }
        public Guid UserId { get; set; }
        public Guid OrderId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public string? MediaUrls { get; set; }
        public string? VendorReply { get; set; }
    }
}
