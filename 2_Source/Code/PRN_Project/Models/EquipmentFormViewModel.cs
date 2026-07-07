using System.ComponentModel.DataAnnotations;

namespace PRN_Project.Models
{
    /// <summary>
    /// ViewModel dùng cho form Thêm mới / Chỉnh sửa thiết bị (UC_AddUpdateEquipment)
    /// </summary>
    public class EquipmentFormViewModel
    {
        public int EquipmentId { get; set; }

        // ----- Bắt buộc -----
        [Required(ErrorMessage = "Mã tài sản không được để trống.")]
        [StringLength(50, ErrorMessage = "Mã tài sản không được vượt quá 50 ký tự.")]
        [RegularExpression(@"^[A-Za-z0-9\-_\.]+$",
            ErrorMessage = "Mã tài sản chỉ được chứa chữ cái (A-Z, a-z), chữ số (0-9) và ký tự - _ .")]
        [Display(Name = "Mã tài sản")]
        public string AssetCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên thiết bị không được để trống.")]
        [StringLength(150, MinimumLength = 3,
            ErrorMessage = "Tên thiết bị phải từ 3 đến 150 ký tự.")]
        [Display(Name = "Tên thiết bị")]
        public string EquipmentName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn loại thiết bị.")]
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn loại thiết bị.")]
        [Display(Name = "Loại thiết bị")]
        public int CategoryId { get; set; }

        // ----- Không bắt buộc -----
        [Display(Name = "Phòng học")]
        public int? CurrentRoomId { get; set; }

        [StringLength(100, ErrorMessage = "Số Serial không được vượt quá 100 ký tự.")]
        [Display(Name = "Số Serial (S/N)")]
        public string? SerialNumber { get; set; }

        [StringLength(150, ErrorMessage = "Hãng sản xuất không được vượt quá 150 ký tự.")]
        [Display(Name = "Hãng sản xuất")]
        public string? Manufacturer { get; set; }

        [StringLength(150, ErrorMessage = "Nhà cung cấp không được vượt quá 150 ký tự.")]
        [Display(Name = "Nhà cung cấp")]
        public string? Supplier { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Ngày mua")]
        public DateOnly? PurchaseDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Hạn bảo hành")]
        public DateOnly? WarrantyExpiry { get; set; }

        [Display(Name = "Ghi chú")]
        public string? Notes { get; set; }

        // ----- Readonly (chỉ hiển thị, không nhập) -----
        public string? Status { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
