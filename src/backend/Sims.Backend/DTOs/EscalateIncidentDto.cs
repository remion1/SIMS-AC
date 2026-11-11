using System.ComponentModel.DataAnnotations;

namespace Sims.Backend.DTOs;

public sealed class EscalateIncidentDto
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "TargetAssignee is required.")]
    [StringLength(200, MinimumLength = 1)]
    public string TargetAssignee { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "Reason is required.")]
    [StringLength(1000, MinimumLength = 1)]
    public string Reason { get; set; } = string.Empty;
}
