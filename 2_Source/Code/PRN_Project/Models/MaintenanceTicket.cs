using System;
using System.Collections.Generic;

namespace PRN_Project.Models;

public partial class MaintenanceTicket
{
    public int TicketId { get; set; }

    public int IncidentId { get; set; }

    public int EquipmentId { get; set; }

    public int CreatedBy { get; set; }

    public string ServiceProvider { get; set; } = null!;

    public decimal? EstimatedCost { get; set; }

    public decimal? ActualCost { get; set; }

    public DateOnly SentDate { get; set; }

    public DateOnly ExpectedReturnDate { get; set; }

    public DateOnly? ActualReturnDate { get; set; }

    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual Equipment Equipment { get; set; } = null!;

    public virtual IncidentReport Incident { get; set; } = null!;
}
