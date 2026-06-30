using System;
using System.Collections.Generic;

namespace PRN_Project.Models;

public partial class Equipment
{
    public int EquipmentId { get; set; }

    public string AssetCode { get; set; } = null!;

    public string EquipmentName { get; set; } = null!;

    public int CategoryId { get; set; }

    public int? CurrentRoomId { get; set; }

    public string? SerialNumber { get; set; }

    public string? Manufacturer { get; set; }

    public string? Supplier { get; set; }

    public DateOnly? PurchaseDate { get; set; }

    public DateOnly? WarrantyExpiry { get; set; }

    public string Status { get; set; } = null!;

    public bool IsActive { get; set; }

    public string? Notes { get; set; }

    public int CreatedBy { get; set; }

    public int? UpdatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual EquipmentCategory Category { get; set; } = null!;

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual Room? CurrentRoom { get; set; }

    public virtual ICollection<DisposalRequest> DisposalRequests { get; set; } = new List<DisposalRequest>();

    public virtual ICollection<EquipmentStatusLog> EquipmentStatusLogs { get; set; } = new List<EquipmentStatusLog>();

    public virtual ICollection<IncidentReport> IncidentReports { get; set; } = new List<IncidentReport>();

    public virtual ICollection<MaintenanceTicket> MaintenanceTickets { get; set; } = new List<MaintenanceTicket>();

    public virtual ICollection<TransferHistory> TransferHistories { get; set; } = new List<TransferHistory>();

    public virtual User? UpdatedByNavigation { get; set; }
}
