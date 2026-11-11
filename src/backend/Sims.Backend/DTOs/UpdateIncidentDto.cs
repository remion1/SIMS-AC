using System.ComponentModel.DataAnnotations;
using Sims.Backend.Models;

namespace Sims.Backend.DTOs;

public sealed class UpdateIncidentDto
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(150, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Description { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string AffectedSystem { get; set; } = string.Empty;

    [RegularExpression(@"^CVE-\d{4}-\d{4,}$",
        ErrorMessage = "CveNumber must follow the format CVE-YYYY-NNNN.")]
    public string? CveNumber { get; set; }

    [Required]
    [EnumDataType(typeof(IncidentSeverity))]
    public IncidentSeverity Severity { get; set; }

    [Required]
    [EnumDataType(typeof(IncidentStatus))]
    public IncidentStatus Status { get; set; }

    public string? AssignedTo { get; set; }
}
