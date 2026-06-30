using System;
using System.Collections.Generic;

namespace PRN_Project.Models;

public partial class User
{
    public int UserId { get; set; }

    public string UserCode { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string? AvatarUrl { get; set; }

    public string Role { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public int FailedLoginCount { get; set; }

    public DateTime? LockoutUntil { get; set; }

    public virtual ICollection<DisposalRequest> DisposalRequestApprovedByNavigations { get; set; } = new List<DisposalRequest>();

    public virtual ICollection<DisposalRequest> DisposalRequestProposedByNavigations { get; set; } = new List<DisposalRequest>();

    public virtual ICollection<Equipment> EquipmentCreatedByNavigations { get; set; } = new List<Equipment>();

    public virtual ICollection<EquipmentStatusLog> EquipmentStatusLogs { get; set; } = new List<EquipmentStatusLog>();

    public virtual ICollection<Equipment> EquipmentUpdatedByNavigations { get; set; } = new List<Equipment>();

    public virtual ICollection<IncidentReport> IncidentReportAssignedToNavigations { get; set; } = new List<IncidentReport>();

    public virtual ICollection<IncidentReport> IncidentReportReportedByNavigations { get; set; } = new List<IncidentReport>();

    public virtual ICollection<MaintenanceTicket> MaintenanceTickets { get; set; } = new List<MaintenanceTicket>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();

    public virtual ICollection<TransferHistory> TransferHistories { get; set; } = new List<TransferHistory>();
}
