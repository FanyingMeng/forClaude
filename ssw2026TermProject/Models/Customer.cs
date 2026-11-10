using System.ComponentModel.DataAnnotations;

namespace ssw2026TermProject.Models
{
    public class Customer
    {
        [Key]
        public int? ID { get; set; }

        [Required(ErrorMessage = "First Name is required.")]
        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [Display(Name = "Middle Name")]
        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Last Name is required.")]
        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        [Required(ErrorMessage = "Date of Birth is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DOB { get; set; }

        [Required(ErrorMessage = "Municipality is required.")]
        [Display(Name = "Municipality (City / Town)")]
        public string? Municipality { get; set; }

        [Required(ErrorMessage = "Province is required.")]
        [Display(Name = "Province")]
        public string? Province { get; set; }

        [Required(ErrorMessage = "Postal Code is required.")]
        [Display(Name = "Postal Code")]
        [RegularExpression(@"^[ABCEGHJ-NPRSTVXY]\d[ABCEGHJ-NPRSTV-Z][\s-]?\d[ABCEGHJ-NPRSTV-Z]\d$", ErrorMessage = "Please enter a valid postal code Eg: A1A 1A1")]
        public string? PostalCode { get; set; }

        [Required(ErrorMessage = "Phone Number is required.")]
        [RegularExpression(@"^(?:\+?1[\s.-]?)?(?:\(\d{3}\)|\d{3})[\s.-]?\d{3}[\s.-]?\d{4}$", ErrorMessage = "Please Enter a valid phone number")]
        [Display(Name = "Phone Number")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "Email Address")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Address is required.")]
        [Display(Name = "Street Address")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Proof of ID is required.")]
        [Display(Name = "ID Verified?")]
        public bool VerifyID { get; set; }
    }
}
