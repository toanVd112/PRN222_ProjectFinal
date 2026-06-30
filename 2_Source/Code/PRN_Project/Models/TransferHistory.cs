using System;
using System.Collections.Generic;

namespace PRN_Project.Models;

public partial class TransferHistory
{
    public int TransferId { get; set; }

    public int EquipmentId { get; set; }

    public int? FromRoomId { get; set; }

    public int ToRoomId { get; set; }

    public int TransferredBy { get; set; }

    public DateTime TransferDate { get; set; }

    public string? Reason { get; set; }

    public virtual Equipment Equipment { get; set; } = null!;

    public virtual Room? FromRoom { get; set; }

    public virtual Room ToRoom { get; set; } = null!;

    public virtual User TransferredByNavigation { get; set; } = null!;
}
