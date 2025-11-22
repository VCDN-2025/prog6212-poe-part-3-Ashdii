using System.ComponentModel.DataAnnotations;

namespace CMCSystem.Models
{
    public class ApprovalLog
    {
        [Key]
        public int Id { get; set; }
        public int ClaimId { get; set; }
        public string ApproverId { get; set; } = string.Empty;
        public string ApproverName { get; set; } = string.Empty;
        public DateTime ActionAt { get; set; } = DateTime.UtcNow;
        public string Action { get; set; } = string.Empty; // Approved / Rejected / Comment
        public string? Comments { get; set; }
    }
}
