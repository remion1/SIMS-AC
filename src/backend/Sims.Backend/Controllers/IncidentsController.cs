using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sims.Backend.Data;
using Sims.Backend.DTOs;
using Sims.Backend.Models;
using Sims.Backend.Services;

namespace Sims.Backend.Controllers;

/// <summary>
/// REST endpoints for managing security/operational incidents.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class IncidentsController(SimsDbContext db, IRedisLogService logger) : ControllerBase
{
    // ── GET /api/incidents ────────────────────────────────────────────────────

    /// <summary>
    /// Returns all incidents, with optional filtering by status and/or severity.
    /// </summary>
    // Alle Incidents abrufen; optional nach Status und Schweregrad filtern
    [HttpGet]
    [ProducesResponseType<IEnumerable<Incident>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] IncidentStatus? status   = null,
        [FromQuery] IncidentSeverity? severity = null,
        CancellationToken ct = default)
    {
        var query = db.Incidents.AsNoTracking();

        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);

        if (severity.HasValue)
            query = query.Where(i => i.Severity == severity.Value);

        var incidents = await query
            .OrderByDescending(i => i.Timestamp)
            .ToListAsync(ct);

        return Ok(incidents);
    }

    // ── GET /api/incidents/{id} ───────────────────────────────────────────────

    /// <summary>Returns a single incident by id, or 404 if not found.</summary>
    // Einzelnen Incident per ID laden; 404 wenn nicht vorhanden
    [HttpGet("{id:int}", Name = nameof(GetById))]
    [ProducesResponseType<Incident>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
    {
        var incident = await db.Incidents
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (incident is null)
            return NotFound(new { Message = $"Incident with id {id} was not found." });

        return Ok(incident);
    }

    // ── POST /api/incidents ───────────────────────────────────────────────────

    /// <summary>Creates a new incident. Status defaults to Open, Timestamp to UtcNow.</summary>
    // Neuen Incident anlegen; Status startet immer als Open
    [HttpPost]
    [ProducesResponseType<Incident>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateIncidentDto dto,
        CancellationToken ct = default)
    {
        var incident = new Incident
        {
            Timestamp      = DateTime.UtcNow,
            Title          = dto.Title,
            Description    = dto.Description,
            AffectedSystem = dto.AffectedSystem,
            CveNumber      = dto.CveNumber,
            Severity       = dto.Severity,
            Status         = IncidentStatus.Open,
            ReportedBy     = dto.ReportedBy,
            AssignedTo     = null
        };

        db.Incidents.Add(incident);
        await db.SaveChangesAsync(ct);

        await logger.LogAsync("INFO",
            $"Incident #{incident.Id} created.",
            $"Title: {incident.Title} | Severity: {incident.Severity} | ReportedBy: {incident.ReportedBy}");

        return CreatedAtAction(nameof(GetById), new { id = incident.Id }, incident);
    }

    // ── PUT /api/incidents/{id} ───────────────────────────────────────────────

    /// <summary>Fully updates a mutable incident. Returns 404 if not found.</summary>
    // Incident vollständig aktualisieren
    [HttpPut("{id:int}")]
    [ProducesResponseType<Incident>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateIncidentDto dto,
        CancellationToken ct = default)
    {
        var incident = await db.Incidents.FindAsync([id], ct);

        if (incident is null)
            return NotFound(new { Message = $"Incident with id {id} was not found." });

        incident.Title          = dto.Title;
        incident.Description    = dto.Description;
        incident.AffectedSystem = dto.AffectedSystem;
        incident.CveNumber      = dto.CveNumber;
        incident.Severity       = dto.Severity;
        incident.Status         = dto.Status;
        incident.AssignedTo     = dto.AssignedTo;

        await db.SaveChangesAsync(ct);

        await logger.LogAsync("INFO",
            $"Incident #{incident.Id} updated.",
            $"Status: {incident.Status} | Severity: {incident.Severity} | AssignedTo: {incident.AssignedTo ?? "—"}");

        return Ok(incident);
    }

    // ── POST /api/incidents/{id}/escalate ─────────────────────────────────────

    /// <summary>
    /// Escalates the incident: sets Status = Escalated, assigns a new owner,
    /// and appends the escalation reason to the description.
    /// </summary>
    [HttpPost("{id:int}/escalate")]
    [ProducesResponseType<Incident>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Escalate(
        int id,
        [FromBody] EscalateIncidentDto dto,
        CancellationToken ct = default)
    {
        var incident = await db.Incidents.FindAsync([id], ct);

        if (incident is null)
            return NotFound(new { Message = $"Incident with id {id} was not found." });

        if (incident.Status == IncidentStatus.Closed)
            return BadRequest(new { Message = "A closed incident cannot be escalated." });

        incident.Status     = IncidentStatus.Escalated;
        incident.AssignedTo = dto.TargetAssignee;
        incident.Description =
            $"{incident.Description}\n\n--- Escalation ({DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC) ---\n" +
            $"Assigned to: {dto.TargetAssignee}\nReason: {dto.Reason}";

        await db.SaveChangesAsync(ct);

        await logger.LogAsync("WARN",
            $"Incident #{incident.Id} escalated to '{dto.TargetAssignee}'.",
            $"Reason: {dto.Reason}");

        return Ok(incident);
    }

    // ── POST /api/incidents/{id}/close ────────────────────────────────────────

    /// <summary>Closes the incident. Idempotent if already closed.</summary>
    [HttpPost("{id:int}/close")]
    [ProducesResponseType<Incident>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Close(int id, CancellationToken ct = default)
    {
        var incident = await db.Incidents.FindAsync([id], ct);

        if (incident is null)
            return NotFound(new { Message = $"Incident with id {id} was not found." });

        if (incident.Status == IncidentStatus.Closed)
            return Ok(incident); // already closed – idempotent

        incident.Status = IncidentStatus.Closed;
        await db.SaveChangesAsync(ct);

        await logger.LogAsync("INFO",
            $"Incident #{incident.Id} closed.",
            $"Title: {incident.Title} | Severity: {incident.Severity}");

        return Ok(incident);
    }
}
