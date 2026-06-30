using System;
using System.Collections.Generic;

namespace PRN_Project.Models;

public partial class IncidentReport
{
    public int IncidentId { get; set; }

    public int EquipmentId { get; set; }

    public int RoomId { get; set; }

    public int ReportedBy { get; set; }

    public int? AssignedTo { get; set; }

    public string Description { get; set; } = null!;

    public string? ImageUrl { get; set; }

    public string Status { get; set; } = null!;

    public DateTime ReportedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public string? ResolutionNote { get; set; }

    public DateTime? DueDate { get; set; }

    public bool IsOverdue { get; set; }

    public virtual User? AssignedToNavigation { get; set; }

    public virtual Equipment Equipment { get; set; } = null!;

    public virtual MaintenanceTicket? MaintenanceTicket { get; set; }

    public virtual User ReportedByNavigation { get; set; } = null!;

    public virtual Room Room { get; set; } = null!;
}
