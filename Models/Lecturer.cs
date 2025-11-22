using System.ComponentModel.DataAnnotations;

namespace CMCSystem.Models
{
    public class Lecturer
    {
        [Key]
        public int LecturerId { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; }

        public string? PhoneNumber { get; set; }

        public decimal HourlyRate { get; set; }
    }

}
