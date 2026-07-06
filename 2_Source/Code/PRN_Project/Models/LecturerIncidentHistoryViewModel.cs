namespace PRN_Project.Models;

public class LecturerIncidentHistoryViewModel
{
    public List<LecturerIncidentHistoryItemViewModel> Incidents { get; set; } = new();
}

public class LecturerIncidentHistoryItemViewModel
{
    public int IncidentId { get; set; }

    public string AssetCode { get; set; } = null!;

    public string EquipmentName { get; set; } = null!;

    public string RoomDisplayName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime ReportedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public string StatusText => Status == "Pending"
        ? "Đang chờ xử lý"
        : Status == "InProgress"
            ? "Đang xử lý"
            : Status == "Resolved"
                ? "Đã xử lý"
                : Status == "Cancelled"
                    ? "Đã hủy"
                    : Status;

    public string StatusBadgeClass => Status == "Pending"
        ? "bg-warning-subtle text-warning border"
        : Status == "InProgress"
            ? "bg-info-subtle text-info border"
            : Status == "Resolved"
                ? "bg-success-subtle text-success border"
                : "bg-light text-dark border";
}

public class LecturerIncidentDetailViewModel
{
    public int IncidentId { get; set; }

    public int RoomId { get; set; }

    public string AssetCode { get; set; } = null!;

    public string EquipmentName { get; set; } = null!;

    public string CategoryName { get; set; } = null!;

    public string RoomDisplayName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime ReportedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public string? ResolutionNote { get; set; }

    public string StatusText => Status == "Pending"
        ? "Đang chờ xử lý"
        : Status == "InProgress"
            ? "Đang xử lý"
            : Status == "Resolved"
                ? "Đã xử lý"
                : Status == "Cancelled"
                    ? "Đã hủy"
                    : Status;

    public string StatusBadgeClass => Status == "Pending"
        ? "bg-warning-subtle text-warning border"
        : Status == "InProgress"
            ? "bg-info-subtle text-info border"
            : Status == "Resolved"
                ? "bg-success-subtle text-success border"
                : "bg-light text-dark border";
}
