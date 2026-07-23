using System;
using System.Collections.Generic;

namespace PRN_Project.Models;

public partial class TransferRequest
{
    public int RequestId { get; set; }

    public int EquipmentId { get; set; }

    public int? FromRoomId { get; set; }

    public int? ToRoomId { get; set; }

    public int ProposedBy { get; set; }

    public int? ApprovedBy { get; set; }

    public string Reason { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime ProposedAt { get; set; }

    public DateTime? DecidedAt { get; set; }

    public string? AdminNote { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual Equipment Equipment { get; set; } = null!;

    public virtual Room? FromRoom { get; set; }

    public virtual User ProposedByNavigation { get; set; } = null!;

    public virtual Room? ToRoom { get; set; }
}
