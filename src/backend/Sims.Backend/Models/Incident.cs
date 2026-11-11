using System.ComponentModel.DataAnnotations;

namespace Sims.Backend.Models;

public sealed class Incident
{
    public int Id { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Required(AllowEmptyStrings = false, ErrorMessage = "Title is required.")]
    [StringLength(150, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 150 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "Description is required.")]
    [MinLength(1, ErrorMessage = "Description must not be empty.")]
    public string Description { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "AffectedSystem is required.")]
    [MinLength(1, ErrorMessage = "AffectedSystem must not be empty.")]
    public string AffectedSystem { get; set; } = string.Empty;

    [RegularExpression(
        @"^CVE-\d{4}-\d{4,}$",
        ErrorMessage = "CveNumber must follow the format CVE-YYYY-NNNN (e.g. CVE-2024-12345).")]
    public string? CveNumber { get; set; }

    [Required]
    [EnumDataType(typeof(IncidentSeverity), ErrorMessage = "Invalid severity value.")]
    public IncidentSeverity Severity { get; set; }

    [EnumDataType(typeof(IncidentStatus), ErrorMessage = "Invalid status value.")]
    public IncidentStatus Status { get; set; } = IncidentStatus.Open;

    [Required(AllowEmptyStrings = false, ErrorMessage = "ReportedBy is required.")]
    [MinLength(1, ErrorMessage = "ReportedBy must not be empty.")]
    public string ReportedBy { get; set; } = string.Empty;

    public string? AssignedTo { get; set; }
}
