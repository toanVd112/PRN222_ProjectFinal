using System.ComponentModel.DataAnnotations;

namespace PRN_Project.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập Mật khẩu")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;
        
        public bool RememberMe { get; set; }
    }
}
