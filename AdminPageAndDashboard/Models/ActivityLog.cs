using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdminPageAndDashboard.Models
{
    [Table("activity_logs")]
    public class ActivityLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("action")]
        public string Action { get; set; } = null!;

        [MaxLength(100)]
        [Column("entity_type")]
        public string? EntityType { get; set; }

        [Column("entity_id")]
        public string? EntityId { get; set; }

        [MaxLength(500)]
        [Column("details")]
        public string? Details { get; set; }

        [MaxLength(45)]
        [Column("ip_address")]
        public string? IpAddress { get; set; }

        [Column("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
    }
}