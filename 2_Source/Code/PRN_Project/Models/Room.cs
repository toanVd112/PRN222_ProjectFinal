using System;
using System.Collections.Generic;

namespace PRN_Project.Models;

public partial class Room
{
    public int RoomId { get; set; }

    public string RoomCode { get; set; } = null!;

    public string RoomName { get; set; } = null!;

    public string? Location { get; set; }

    public int? Capacity { get; set; }

    public string? RoomType { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();

    public virtual ICollection<IncidentReport> IncidentReports { get; set; } = new List<IncidentReport>();

    public virtual ICollection<TransferHistory> TransferHistoryFromRooms { get; set; } = new List<TransferHistory>();

    public virtual ICollection<TransferHistory> TransferHistoryToRooms { get; set; } = new List<TransferHistory>();
}
