using System.ComponentModel.DataAnnotations;

namespace EmailContentApi.Models
{
    public class EmailContent
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(10000)] // Adjust max length as needed for your use case
        public string Content { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
} 