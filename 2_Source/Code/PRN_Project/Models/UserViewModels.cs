using PRN_Project.Models;
using System.Collections.Generic;

namespace PRN_Project.Models
{
    public class UserListViewModel
    {
        public IEnumerable<User> Users { get; set; } = new List<User>();
        public string? SearchTerm { get; set; }
        public string? RoleFilter { get; set; }
        public string? StatusFilter { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class CreateUserViewModel
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập Mã người dùng")]
        [System.ComponentModel.DataAnnotations.StringLength(8, ErrorMessage = "Mã người dùng không vượt quá 8 ký tự")]
        public string UserCode { get; set; } = null!;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập Họ và Tên")]
        [System.ComponentModel.DataAnnotations.StringLength(150, ErrorMessage = "Họ và Tên không vượt quá 150 ký tự")]
        public string FullName { get; set; } = null!;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập Email")]
        [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = null!;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng chọn Vai trò")]
        public string Role { get; set; } = null!;

        public List<int> AssignedRoomIds { get; set; } = new List<int>();
        public IEnumerable<RoomOptionViewModel> AvailableRooms { get; set; } = new List<RoomOptionViewModel>();
    }

    public class EditUserViewModel
    {
        public int UserId { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập Mã người dùng")]
        [System.ComponentModel.DataAnnotations.StringLength(8, ErrorMessage = "Mã người dùng không vượt quá 8 ký tự")]
        public string UserCode { get; set; } = null!;

        public string? FullName { get; set; } // Chỉ để hiển thị, không post lại

        public string Email { get; set; } = null!;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng chọn Vai trò")]
        public string Role { get; set; } = null!;

        public List<int> AssignedRoomIds { get; set; } = new List<int>();
        public IEnumerable<RoomOptionViewModel> AvailableRooms { get; set; } = new List<RoomOptionViewModel>();
    }

    public class UserDetailsViewModel
    {
        public int UserId { get; set; }
        public string UserCode { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
        public bool IsActive { get; set; }
        public System.DateTime CreatedAt { get; set; }
        public string? AvatarUrl { get; set; }
        public List<string> AssignedRooms { get; set; } = new List<string>();
    }
}
