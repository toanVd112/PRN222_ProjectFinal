using System.ComponentModel.DataAnnotations;

namespace PRN_Project.Models
{
    public class EquipmentProposeDisposalViewModel
    {
        [Required]
        public int EquipmentId { get; set; }

        [Display(Name = "Mã tài sản")]
        public string AssetCode { get; set; } = null!;

        [Display(Name = "Tên thiết bị")]
        public string EquipmentName { get; set; } = null!;

        [Display(Name = "Vị trí hiện tại")]
        public string RoomDisplayName { get; set; } = null!;

        [Display(Name = "Trạng thái hiện tại")]
        public string Status { get; set; } = null!;

        [Required(ErrorMessage = "Lý do đề xuất thanh lý là bắt buộc.")]
        [MinLength(10, ErrorMessage = "Lý do phải có ít nhất 10 ký tự.")]
        [Display(Name = "Lý do đề xuất thanh lý")]
        public string Reason { get; set; } = null!;
    }

    public class ReviewDisposalRequestViewModel
    {
        [Required]
        public int DisposalId { get; set; }
        
        [Required]
        public string Action { get; set; } = null!; // "Approve" or "Reject"
        
        [StringLength(500)]
        public string? AdminNote { get; set; }
    }
}
