using System;
using System.ComponentModel.DataAnnotations;

namespace PRN_Project.Models
{
    public class MaintenanceTicketViewModel
    {
        [Required]
        public int IncidentId { get; set; }

        [Required]
        public int EquipmentId { get; set; }

        [Display(Name = "Mã tài sản")]
        public string? AssetCode { get; set; }

        [Display(Name = "Tên thiết bị")]
        public string? EquipmentName { get; set; }

        [Required(ErrorMessage = "Đơn vị cung cấp dịch vụ bảo trì là bắt buộc.")]
        [StringLength(150, ErrorMessage = "Đơn vị bảo trì không được vượt quá 150 ký tự.")]
        [Display(Name = "Đơn vị nhận bảo trì/Sửa chữa")]
        public string ServiceProvider { get; set; } = null!;

        [Range(0, 999999999, ErrorMessage = "Chi phí ước tính phải là số dương.")]
        [Display(Name = "Chi phí ước tính (VNĐ)")]
        public decimal? EstimatedCost { get; set; }

        [Required(ErrorMessage = "Ngày dự kiến nhận lại máy là bắt buộc.")]
        [Display(Name = "Ngày dự kiến trả máy")]
        public DateOnly ExpectedReturnDate { get; set; }
    }
}
