using System;
using System.Collections.Generic;

namespace PRN_Project.Models;

public partial class VwEquipmentDetail
{
    public int EquipmentId { get; set; }

    public string AssetCode { get; set; } = null!;

    public string EquipmentName { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateOnly? PurchaseDate { get; set; }

    public DateOnly? WarrantyExpiry { get; set; }

    public string? CategoryName { get; set; }

    public string? RoomCode { get; set; }

    public string? RoomName { get; set; }

    public string? CreatedByName { get; set; }
}
