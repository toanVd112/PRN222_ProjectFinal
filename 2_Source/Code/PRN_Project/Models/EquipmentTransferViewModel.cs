using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PRN_Project.Models
{
    public class EquipmentTransferViewModel
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

        [Required(ErrorMessage = "Vui lòng chọn phòng học đích.")]
        [Display(Name = "Phòng học điều chuyển đến")]
        public int ToRoomId { get; set; }

        [Required(ErrorMessage = "Lý do điều chuyển là bắt buộc.")]
        [StringLength(500, ErrorMessage = "Lý do không được vượt quá 500 ký tự.")]
        [MinLength(5, ErrorMessage = "Lý do phải có ít nhất 5 ký tự.")]
        [Display(Name = "Lý do điều chuyển")]
        public string Reason { get; set; } = null!;

        public List<SelectListItem> Rooms { get; set; } = new();
    }
}
