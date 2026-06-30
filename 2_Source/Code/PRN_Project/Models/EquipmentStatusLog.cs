using System;
using System.Collections.Generic;

namespace PRN_Project.Models;

public partial class EquipmentStatusLog
{
    public int LogId { get; set; }

    public int EquipmentId { get; set; }

    public int ChangedBy { get; set; }

    public string? OldStatus { get; set; }

    public string NewStatus { get; set; } = null!;

    public string? FieldChanged { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string? ChangeReason { get; set; }

    public DateTime ChangedAt { get; set; }

    public virtual User ChangedByNavigation { get; set; } = null!;

    public virtual Equipment Equipment { get; set; } = null!;
}
