using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Models
{
    public class PointHistory : AuditableEntity
    {
        [Key] public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? OrderId { get; set; }
        public int PointsEarned { get; set; }
        public int PointsSpent { get; set; }
        [StringLength(255)] public string? Description { get; set; }
    }
}
