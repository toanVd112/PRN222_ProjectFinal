using System;
using System.Collections.Generic;

namespace PRN_Project.Models;

public partial class RoomStatusLog
{
    public int LogId { get; set; }

    public int RoomId { get; set; }

    public int ChangedBy { get; set; }

    public bool? OldStatus { get; set; }

    public bool NewStatus { get; set; }

    public string? ChangeReason { get; set; }

    public DateTime ChangedAt { get; set; }

    public virtual User ChangedByNavigation { get; set; } = null!;

    public virtual Room Room { get; set; } = null!;
}
