// buronet_service.Models.User/TagFrequency.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace buronet_service.Models.User
{
    [Table("TagFrequencies")]
    public class TagFrequency
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)] // Max length for a tag name
        public string TagName { get; set; } = string.Empty;

        [Required]
        public int Frequency { get; set; } // How many times this tag has been used recently

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow; // When this frequency was last updated
    }
}