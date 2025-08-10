using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace buronet_service.Models.User
{
    [Table("Connections")]
    public class Connection
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "char(36)")]
        public Guid UserId1 { get; set; }

        [Required]
        [Column(TypeName = "char(36)")]
        public Guid UserId2 { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId1")]
        [InverseProperty("ConnectionsMade")]
        public User User1 { get; set; } = null!;

        [ForeignKey("UserId2")]
        [InverseProperty("ConnectionsReceived")]
        public User User2 { get; set; } = null!;
    }
}