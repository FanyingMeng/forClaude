using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ssw2026TermProject.Models
{
    public class UploadedImage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UploadedImageId { get; set; }

       
        [Required]
        public string? FilePath { get; set; }

        public string? Description { get; set; }

        
        public string? AltText { get; set; }

        public DateTime UploadDateTime { get; set; } = DateTime.Now;

       
        [Required]
        public string? StaffMemberId { get; set; }

        public bool IsApproved { get; set; } = false;
    
    }
}
