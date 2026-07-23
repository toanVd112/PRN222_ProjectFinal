using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PRN_Project.Models;

public class ProposeTransferViewModel
{
    [Required]
    public int EquipmentId { get; set; }

    [Display(Name = "Mã tài sản")]
    public string AssetCode { get; set; } = null!;

    [Display(Name = "Tên thiết bị")]
    public string EquipmentName { get; set; } = null!;

    public int? CurrentRoomId { get; set; }

    [Display(Name = "Phòng học hiện tại")]
    public string CurrentRoomDisplayName { get; set; } = null!;

    [Display(Name = "Phòng học đích (Để trống nếu luân chuyển về Kho)")]
    public int? ToRoomId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập lý do đề xuất.")]
    [StringLength(500, ErrorMessage = "Lý do không được vượt quá 500 ký tự.")]
    [Display(Name = "Lý do")]
    public string Reason { get; set; } = null!;

    public List<SelectListItem> Rooms { get; set; } = new();
}

public class TransferRequestListItemViewModel
{
    public int RequestId { get; set; }
    public int EquipmentId { get; set; }
    public string AssetCode { get; set; } = null!;
    public string EquipmentName { get; set; } = null!;
    public string FromRoomDisplayName { get; set; } = null!;
    public string ToRoomDisplayName { get; set; } = null!;
    public string ProposedBy { get; set; } = null!;
    public DateTime ProposedAt { get; set; }
    public string Reason { get; set; } = null!;
    public string Status { get; set; } = null!;
    
    // Details for processed requests
    public string? AdminNote { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? ApprovedBy { get; set; }
}

public class TransferRequestListViewModel
{
    public List<TransferRequestListItemViewModel> Requests { get; set; } = new();
    public string? StatusFilter { get; set; }
}

public class ReviewTransferRequestViewModel
{
    [Required]
    public int RequestId { get; set; }
    
    [Required]
    public string Action { get; set; } = null!; // "Approve" or "Reject"
    
    public string? AdminNote { get; set; }
}
