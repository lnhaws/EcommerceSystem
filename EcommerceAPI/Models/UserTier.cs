using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Models
{
    // --- LOYALTY ---
    public class UserTier : AuditableEntity
    {
        [Key] public Guid UserId { get; set; }
        [StringLength(50)] public string CurrentTier { get; set; } = "Bronze";
        public int TotalPoints { get; set; }
        public DateTime LastEvaluated { get; set; }
    }
}
