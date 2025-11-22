using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

//Reference List
//Title: Pro ASP.NET Core MVC 2
//Author: Adam Freeman
//Date: 2017
//Edition: 7th ed
//Publisher: Apress

namespace CMCSystem.Models
{
    public class Claim
    {
        [Key]
        public int ClaimId { get; set; }

        [Required(ErrorMessage = "Lecturer name is required.")]
        public string LecturerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hours worked is required.")]
        [Range(0.1, 1000, ErrorMessage = "Hours worked must be between 0.1 and 1000.")]
        public decimal HoursWorked { get; set; }

        [Required(ErrorMessage = "Hourly rate is required.")]
        [Range(0.1, 1000, ErrorMessage = "Hourly rate must be between 0.1 and 1000.")]
        public decimal HourlyRate { get; set; }

        public decimal TotalAmount { get; set; }

        [Required]
        public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

        [Required]
        public DateTime DateSubmitted { get; set; } = DateTime.UtcNow;

        public int? LecturerId { get; set; }

        public string SupportingDocumentPath { get; set; }
        public Lecturer? Lecturer { get; set; }

        public string? RejectionComments { get; set; }
        public DateTime? RejectedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }
}