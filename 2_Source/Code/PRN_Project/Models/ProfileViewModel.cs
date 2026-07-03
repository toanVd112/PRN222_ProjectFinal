using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PRN_Project.Models
{
    public class ProfileViewModel
    {
        public string UserCode { get; set; } = string.Empty;

        [Display(Name = "Họ và tên")]
        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [RegularExpression(@"^(?:\s*\S){1,40}\s*$", ErrorMessage = "Họ và tên không được để trống và tối đa 40 ký tự (không tính khoảng trắng).")]
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        [Display(Name = "Số điện thoại")]
        [RegularExpression(@"^\s*0[0-9]{9}\s*$", ErrorMessage = "Số điện thoại không hợp lệ. Vui lòng nhập 10 chữ số bắt đầu bằng số 0.")]
        public string? PhoneNumber { get; set; }

        public string? AvatarUrl { get; set; }

        [Display(Name = "Tải lên ảnh mới")]
        public IFormFile? AvatarFile { get; set; }
    }
}
