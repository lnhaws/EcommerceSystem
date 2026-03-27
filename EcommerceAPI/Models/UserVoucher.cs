using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Models
{
    public class UserVoucher : AuditableEntity
    {
        [Key] public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid VoucherId { get; set; }
        public bool IsUsed { get; set; }
        public DateTime? UsedAt { get; set; }
    }
}
