using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PRN_Project.Models
{
    public class EquipmentCreateViewModel
    {
        [Required(ErrorMessage = "Mã tài sản (Asset Code) là bắt buộc.")]
        [StringLength(50, ErrorMessage = "Mã tài sản không được vượt quá 50 ký tự.")]
        [RegularExpression(@"^[a-zA-Z0-9_\-]+$", ErrorMessage = "Mã tài sản chỉ gồm chữ cái, chữ số, dấu gạch ngang và gạch dưới.")]
        [Display(Name = "Mã tài sản (Asset Code)")]
        public string AssetCode { get; set; } = null!;

        [Required(ErrorMessage = "Tên thiết bị là bắt buộc.")]
        [StringLength(150, ErrorMessage = "Tên thiết bị không được vượt quá 150 ký tự.")]
        [Display(Name = "Tên thiết bị")]
        public string EquipmentName { get; set; } = null!;

        [Required(ErrorMessage = "Loại thiết bị là bắt buộc.")]
        [Display(Name = "Loại thiết bị")]
        public int CategoryId { get; set; }

        [Display(Name = "Phòng học hiện tại")]
        public int? CurrentRoomId { get; set; }

        [StringLength(100, ErrorMessage = "Số Serial không được vượt quá 100 ký tự.")]
        [Display(Name = "Số Serial")]
        public string? SerialNumber { get; set; }

        [StringLength(150, ErrorMessage = "Nhà sản xuất không được vượt quá 150 ký tự.")]
        [Display(Name = "Nhà sản xuất")]
        public string? Manufacturer { get; set; }

        [StringLength(150, ErrorMessage = "Nhà cung cấp không được vượt quá 150 ký tự.")]
        [Display(Name = "Nhà cung cấp")]
        public string? Supplier { get; set; }

        [Display(Name = "Ngày mua")]
        public DateOnly? PurchaseDate { get; set; }

        [Display(Name = "Ngày hết hạn bảo hành")]
        public DateOnly? WarrantyExpiry { get; set; }

        [Display(Name = "Ghi chú")]
        public string? Notes { get; set; }

        public List<SelectListItem> Categories { get; set; } = new();
        public List<SelectListItem> Rooms { get; set; } = new();
    }
}
