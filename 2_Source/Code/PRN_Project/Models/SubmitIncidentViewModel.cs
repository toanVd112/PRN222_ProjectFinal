using System.ComponentModel.DataAnnotations;

namespace PRN_Project.Models;

public class SubmitIncidentViewModel
{
    public int EquipmentId { get; set; }

    public int RoomId { get; set; }

    public string AssetCode { get; set; } = null!;

    public string EquipmentName { get; set; } = null!;

    public string CategoryName { get; set; } = null!;

    public string RoomDisplayName { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập nội dung mô tả sự cố.")]
    [StringLength(1000, ErrorMessage = "Mô tả sự cố không được vượt quá 1000 ký tự.")]
    [Display(Name = "Mô tả sự cố")]
    public string Description { get; set; } = string.Empty;
}
