using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace PRN_Project.Models;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<DisposalRequest> DisposalRequests { get; set; }

    public virtual DbSet<Equipment> Equipments { get; set; }

    public virtual DbSet<EquipmentCategory> EquipmentCategories { get; set; }

    public virtual DbSet<EquipmentStatusLog> EquipmentStatusLogs { get; set; }

    public virtual DbSet<IncidentReport> IncidentReports { get; set; }

    public virtual DbSet<MaintenanceTicket> MaintenanceTickets { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<TransferHistory> TransferHistories { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VwDashboardStat> VwDashboardStats { get; set; }

    public virtual DbSet<VwEquipmentDetail> VwEquipmentDetails { get; set; }

    public virtual DbSet<VwPendingIncident> VwPendingIncidents { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS;Database=PRN_Project;User Id=sa;Password=123456;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DisposalRequest>(entity =>
        {
            entity.HasKey(e => e.DisposalId);

            entity.HasIndex(e => new { e.EquipmentId, e.Status }, "IX_Disposal_Equipment");

            entity.HasIndex(e => new { e.Status, e.ProposedAt }, "IX_Disposal_Status").IsDescending(false, true);

            entity.Property(e => e.DisposalId).HasColumnName("DisposalID");
            entity.Property(e => e.AdminNote).HasMaxLength(500);
            entity.Property(e => e.DecidedAt).HasPrecision(0);
            entity.Property(e => e.EquipmentId).HasColumnName("EquipmentID");
            entity.Property(e => e.ProposedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.DisposalRequestApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK_Disposal_ApprovedBy");

            entity.HasOne(d => d.Equipment).WithMany(p => p.DisposalRequests)
                .HasForeignKey(d => d.EquipmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Disposal_Equipment");

            entity.HasOne(d => d.ProposedByNavigation).WithMany(p => p.DisposalRequestProposedByNavigations)
                .HasForeignKey(d => d.ProposedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Disposal_ProposedBy");
        });

        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("trg_Equipment_AfterUpdate"));

            entity.HasIndex(e => e.CategoryId, "IX_Equipments_Category").HasFilter("([IsActive]=(1))");

            entity.HasIndex(e => e.CurrentRoomId, "IX_Equipments_Room").HasFilter("([CurrentRoomID] IS NOT NULL AND [IsActive]=(1))");

            entity.HasIndex(e => e.Status, "IX_Equipments_Status").HasFilter("([IsActive]=(1))");

            entity.HasIndex(e => e.WarrantyExpiry, "IX_Equipments_Warranty").HasFilter("([WarrantyExpiry] IS NOT NULL AND [Status]<>'Disposed' AND [IsActive]=(1))");

            entity.HasIndex(e => e.AssetCode, "UIX_Equipments_AssetCode").IsUnique();

            entity.HasIndex(e => e.AssetCode, "UQ_Equipments_AssetCode").IsUnique();

            entity.Property(e => e.EquipmentId).HasColumnName("EquipmentID");
            entity.Property(e => e.AssetCode).HasMaxLength(50);
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.CurrentRoomId).HasColumnName("CurrentRoomID");
            entity.Property(e => e.EquipmentName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Manufacturer).HasMaxLength(150);
            entity.Property(e => e.SerialNumber).HasMaxLength(100);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("InUse");
            entity.Property(e => e.Supplier).HasMaxLength(150);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Category).WithMany(p => p.Equipment)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Equipments_Category");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.EquipmentCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Equipments_CreatedBy");

            entity.HasOne(d => d.CurrentRoom).WithMany(p => p.Equipment)
                .HasForeignKey(d => d.CurrentRoomId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Equipments_Room");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.EquipmentUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("FK_Equipments_UpdatedBy");
        });

        modelBuilder.Entity<EquipmentCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId);

            entity.HasIndex(e => e.CategoryName, "UQ_EquipmentCategories_Name").IsUnique();

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CategoryName).HasMaxLength(150);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<EquipmentStatusLog>(entity =>
        {
            entity.HasKey(e => e.LogId);

            entity.HasIndex(e => new { e.ChangedBy, e.ChangedAt }, "IX_StatusLog_ChangedBy").IsDescending(false, true);

            entity.HasIndex(e => new { e.EquipmentId, e.ChangedAt }, "IX_StatusLog_Equipment").IsDescending(false, true);

            entity.Property(e => e.LogId).HasColumnName("LogID");
            entity.Property(e => e.ChangeReason).HasMaxLength(500);
            entity.Property(e => e.ChangedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.EquipmentId).HasColumnName("EquipmentID");
            entity.Property(e => e.FieldChanged).HasMaxLength(100);
            entity.Property(e => e.NewStatus).HasMaxLength(30);
            entity.Property(e => e.NewValue).HasMaxLength(500);
            entity.Property(e => e.OldStatus).HasMaxLength(30);
            entity.Property(e => e.OldValue).HasMaxLength(500);

            entity.HasOne(d => d.ChangedByNavigation).WithMany(p => p.EquipmentStatusLogs)
                .HasForeignKey(d => d.ChangedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StatusLog_ChangedBy");

            entity.HasOne(d => d.Equipment).WithMany(p => p.EquipmentStatusLogs)
                .HasForeignKey(d => d.EquipmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StatusLog_Equipment");
        });

        modelBuilder.Entity<IncidentReport>(entity =>
        {
            entity.HasKey(e => e.IncidentId);

            entity.HasIndex(e => new { e.EquipmentId, e.Status }, "IX_Incident_Equipment");

            entity.HasIndex(e => e.DueDate, "IX_Incident_Overdue").HasFilter("([Status] IN ('Pending', 'InProgress'))");

            entity.HasIndex(e => new { e.ReportedBy, e.ReportedAt }, "IX_Incident_ReportedBy").IsDescending(false, true);

            entity.HasIndex(e => new { e.Status, e.ReportedAt }, "IX_Incident_Status").IsDescending(false, true);

            entity.Property(e => e.IncidentId).HasColumnName("IncidentID");
            entity.Property(e => e.DueDate).HasPrecision(0);
            entity.Property(e => e.EquipmentId).HasColumnName("EquipmentID");
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.ReportedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ResolvedAt).HasPrecision(0);
            entity.Property(e => e.RoomId).HasColumnName("RoomID");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.AssignedToNavigation).WithMany(p => p.IncidentReportAssignedToNavigations)
                .HasForeignKey(d => d.AssignedTo)
                .HasConstraintName("FK_Incident_AssignedTo");

            entity.HasOne(d => d.Equipment).WithMany(p => p.IncidentReports)
                .HasForeignKey(d => d.EquipmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Incident_Equipment");

            entity.HasOne(d => d.ReportedByNavigation).WithMany(p => p.IncidentReportReportedByNavigations)
                .HasForeignKey(d => d.ReportedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Incident_ReportedBy");

            entity.HasOne(d => d.Room).WithMany(p => p.IncidentReports)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Incident_Room");
        });

        modelBuilder.Entity<MaintenanceTicket>(entity =>
        {
            entity.HasKey(e => e.TicketId);

            entity.HasIndex(e => new { e.EquipmentId, e.CreatedAt }, "IX_Ticket_Equipment").IsDescending(false, true);

            entity.HasIndex(e => new { e.Status, e.SentDate }, "IX_Ticket_Status").IsDescending(false, true);

            entity.HasIndex(e => e.IncidentId, "UQ_Ticket_Incident").IsUnique();

            entity.Property(e => e.TicketId).HasColumnName("TicketID");
            entity.Property(e => e.ActualCost).HasColumnType("decimal(15, 2)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.EquipmentId).HasColumnName("EquipmentID");
            entity.Property(e => e.EstimatedCost).HasColumnType("decimal(15, 2)");
            entity.Property(e => e.IncidentId).HasColumnName("IncidentID");
            entity.Property(e => e.ServiceProvider).HasMaxLength(200);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Sent");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.MaintenanceTickets)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ticket_CreatedBy");

            entity.HasOne(d => d.Equipment).WithMany(p => p.MaintenanceTickets)
                .HasForeignKey(d => d.EquipmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ticket_Equipment");

            entity.HasOne(d => d.Incident).WithOne(p => p.MaintenanceTicket)
                .HasForeignKey<MaintenanceTicket>(d => d.IncidentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ticket_Incident");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasIndex(e => new { e.RecipientId, e.IsRead, e.SentAt }, "IX_Notif_Unread").IsDescending(false, false, true);

            entity.HasIndex(e => new { e.RecipientId, e.SentAt }, "IX_Notif_User").IsDescending(false, true);

            entity.Property(e => e.NotificationId).HasColumnName("NotificationID");
            entity.Property(e => e.RecipientId).HasColumnName("RecipientID");
            entity.Property(e => e.RelatedEntityId).HasColumnName("RelatedEntityID");
            entity.Property(e => e.RelatedEntityType).HasMaxLength(50);
            entity.Property(e => e.SentAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Type).HasMaxLength(50);

            entity.HasOne(d => d.Recipient).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.RecipientId)
                .HasConstraintName("FK_Notif_Recipient");
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.TokenId);

            entity.HasIndex(e => e.ExpiresAt, "IX_ResetToken_Expiry").HasFilter("([IsUsed]=(0))");

            entity.HasIndex(e => e.Token, "IX_ResetToken_Token").HasFilter("([IsUsed]=(0))");

            entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "IX_ResetToken_UserCreated").IsDescending(false, true);

            entity.HasIndex(e => e.Token, "UQ_ResetToken").IsUnique();

            entity.Property(e => e.TokenId).HasColumnName("TokenID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ExpiresAt).HasPrecision(0);
            entity.Property(e => e.Token).HasMaxLength(256);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.PasswordResetTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_ResetToken_User");
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasIndex(e => new { e.IsActive, e.RoomCode }, "IX_Rooms_IsActive");

            entity.HasIndex(e => e.RoomCode, "UQ_Rooms_RoomCode").IsUnique();

            entity.Property(e => e.RoomId).HasColumnName("RoomID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.RoomCode).HasMaxLength(20);
            entity.Property(e => e.RoomName).HasMaxLength(150);
            entity.Property(e => e.RoomType).HasMaxLength(50);
        });

        modelBuilder.Entity<TransferHistory>(entity =>
        {
            entity.HasKey(e => e.TransferId);

            entity.HasIndex(e => new { e.EquipmentId, e.TransferDate }, "IX_Transfer_Equipment").IsDescending(false, true);

            entity.HasIndex(e => new { e.ToRoomId, e.TransferDate }, "IX_Transfer_ToRoom").IsDescending(false, true);

            entity.Property(e => e.TransferId).HasColumnName("TransferID");
            entity.Property(e => e.EquipmentId).HasColumnName("EquipmentID");
            entity.Property(e => e.FromRoomId).HasColumnName("FromRoomID");
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.ToRoomId).HasColumnName("ToRoomID");
            entity.Property(e => e.TransferDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Equipment).WithMany(p => p.TransferHistories)
                .HasForeignKey(d => d.EquipmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Transfer_Equipment");

            entity.HasOne(d => d.FromRoom).WithMany(p => p.TransferHistoryFromRooms)
                .HasForeignKey(d => d.FromRoomId)
                .HasConstraintName("FK_Transfer_FromRoom");

            entity.HasOne(d => d.ToRoom).WithMany(p => p.TransferHistoryToRooms)
                .HasForeignKey(d => d.ToRoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Transfer_ToRoom");

            entity.HasOne(d => d.TransferredByNavigation).WithMany(p => p.TransferHistories)
                .HasForeignKey(d => d.TransferredBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Transfer_TransferredBy");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email, "IX_Users_Email").HasFilter("([IsActive]=(1))");

            entity.HasIndex(e => e.LockoutUntil, "IX_Users_Lockout").HasFilter("([LockoutUntil] IS NOT NULL)");

            entity.HasIndex(e => e.Role, "IX_Users_Role").HasFilter("([IsActive]=(1))");

            entity.HasIndex(e => e.Email, "UQ_Users_Email").IsUnique();

            entity.HasIndex(e => e.UserCode, "UQ_Users_UserCode").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastLoginAt).HasPrecision(0);
            entity.Property(e => e.LockoutUntil).HasPrecision(0);
            entity.Property(e => e.PasswordHash).HasMaxLength(256);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Role).HasMaxLength(20);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
            entity.Property(e => e.UserCode).HasMaxLength(20);
        });

        modelBuilder.Entity<VwDashboardStat>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_DashboardStats");
        });

        modelBuilder.Entity<VwEquipmentDetail>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_EquipmentDetails");

            entity.Property(e => e.AssetCode).HasMaxLength(50);
            entity.Property(e => e.CategoryName).HasMaxLength(150);
            entity.Property(e => e.CreatedByName).HasMaxLength(150);
            entity.Property(e => e.EquipmentId).HasColumnName("EquipmentID");
            entity.Property(e => e.EquipmentName).HasMaxLength(150);
            entity.Property(e => e.RoomCode).HasMaxLength(20);
            entity.Property(e => e.RoomName).HasMaxLength(150);
            entity.Property(e => e.Status).HasMaxLength(30);
        });

        modelBuilder.Entity<VwPendingIncident>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_PendingIncidents");

            entity.Property(e => e.AssetCode).HasMaxLength(50);
            entity.Property(e => e.DueDate).HasPrecision(0);
            entity.Property(e => e.EquipmentName).HasMaxLength(150);
            entity.Property(e => e.IncidentId).HasColumnName("IncidentID");
            entity.Property(e => e.ReportedAt).HasPrecision(0);
            entity.Property(e => e.ReportedByName).HasMaxLength(150);
            entity.Property(e => e.RoomCode).HasMaxLength(20);
            entity.Property(e => e.RoomName).HasMaxLength(150);
            entity.Property(e => e.Status).HasMaxLength(30);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
