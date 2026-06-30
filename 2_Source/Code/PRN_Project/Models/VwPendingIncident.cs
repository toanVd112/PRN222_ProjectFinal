using System;
using System.Collections.Generic;

namespace PRN_Project.Models;

public partial class VwPendingIncident
{
    public int IncidentId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime ReportedAt { get; set; }

    public DateTime? DueDate { get; set; }

    public bool IsOverdue { get; set; }

    public string AssetCode { get; set; } = null!;

    public string EquipmentName { get; set; } = null!;

    public string RoomCode { get; set; } = null!;

    public string RoomName { get; set; } = null!;

    public string ReportedByName { get; set; } = null!;
}
