using System;
using System.Collections.Generic;

namespace PRN_Project.Models;

public partial class DisposalRequest
{
    public int DisposalId { get; set; }

    public int EquipmentId { get; set; }

    public int ProposedBy { get; set; }

    public int? ApprovedBy { get; set; }

    public string Reason { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime ProposedAt { get; set; }

    public DateTime? DecidedAt { get; set; }

    public string? AdminNote { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual Equipment Equipment { get; set; } = null!;

    public virtual User ProposedByNavigation { get; set; } = null!;
}
