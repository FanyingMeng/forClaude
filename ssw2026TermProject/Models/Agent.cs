using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ssw2026TermProject.Models
{
    public class Agent
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AgentId { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [Display(Name = "Middle Name (Optional)")]
        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        [Required(ErrorMessage = "Username is required.")]
        [Display(Name = "Username")]
        public string? Username { get; set; }

        [Required(ErrorMessage = "SIN is required.")]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "The SIN must be a valid 9-digit number.")]

        [Display(Name = "SIN")]
        public string? SIN { get; set; }

        [Required(ErrorMessage = "Date of Birth is required.")]
        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Street Address is required.")]
        [Display(Name = "Street Address")]
        public string? StreetAddress { get; set; }

        [Required(ErrorMessage = "City is required.")]
        [Display(Name = "City")]
        public string? City { get; set; }

        [Required(ErrorMessage = "Province is required.")]
        [Display(Name = "Province")]
        public string? Province { get; set; }

        [Required(ErrorMessage = "Postal Code is required.")]
        [Display(Name = "Postal Code")]
        [RegularExpression(@"^[ABCEGHJ-NPRSTVXY]\d[ABCEGHJ-NPRSTV-Z][\s-]?\d[ABCEGHJ-NPRSTV-Z]\d$", ErrorMessage = "Please enter a valid postal code Eg: A1A 1A1")]
        public string? PostalCode { get; set; }

        [Required(ErrorMessage = "Mobile phone is required.")]
        [Display(Name = "Mobile Phone")]
        [RegularExpression(@"^(?:\+?1[\s.-]?)?(?:\(\d{3}\)|\d{3})[\s.-]?\d{3}[\s.-]?\d{4}$", ErrorMessage = "Please Enter a valid phone number")]
        public string? MobilePhone { get; set; }

        [Required(ErrorMessage = "Office phone is required.")]
        [Display(Name = "Office Phone")]
        [RegularExpression(@"^(?:\+?1[\s.-]?)?(?:\(\d{3}\)|\d{3})[\s.-]?\d{3}[\s.-]?\d{4}$", ErrorMessage = "Please Enter a valid phone number")]
        public string? OfficePhone { get; set; }

        [Display(Name = "Home Phone")]
        [RegularExpression(@"^(?:\+?1[\s.-]?)?(?:\(\d{3}\)|\d{3})[\s.-]?\d{3}[\s.-]?\d{4}$", ErrorMessage = "Please Enter a valid phone number")]
        public string? HomePhone { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [Display(Name = "Email")]
        [DataType(DataType.EmailAddress)]
        public string? Email { get; set; }

        [Display(Name = "Government-issued ID Confirmation")]
        public bool HasVerifiedGovernmentId { get; set; }

        [Display(Name = "Date of Employment")]
        [DataType(DataType.Date)]
        public DateTime DateOfEmployment { get; set; }

        [Display(Name = "Created By Staff ID")]
        public string? CreatedByStaffId { get; set; }
    }
}
