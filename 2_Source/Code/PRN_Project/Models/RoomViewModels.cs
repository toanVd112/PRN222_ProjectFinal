using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PRN_Project.Models
{
    public class RoomListViewModel
    {
        public List<Room> Rooms { get; set; } = new List<Room>();
        
        public string SearchTerm { get; set; }
        public string StatusFilter { get; set; }
        public string TypeFilter { get; set; }

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
    }

    public class CreateRoomViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Mã phòng không được để trống")]
        [StringLength(20, ErrorMessage = "Mã phòng không vượt quá 20 ký tự")]
        public string RoomCode { get; set; }

        [Required(ErrorMessage = "Loại phòng không được để trống")]
        [StringLength(50, ErrorMessage = "Loại phòng không vượt quá 50 ký tự")]
        public string RoomType { get; set; }

        [Required(ErrorMessage = "Vị trí không được để trống")]
        [StringLength(200, ErrorMessage = "Vị trí không vượt quá 200 ký tự")]
        public string Location { get; set; }

        [Range(1, 1000, ErrorMessage = "Sức chứa phải từ 1 đến 1000")]
        public int? Capacity { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if ((RoomType == "Phòng học (P)" || RoomType == "Phòng LAB") && Capacity > 35)
            {
                yield return new ValidationResult("Phòng học và phòng LAB chỉ được chứa tối đa 35 người.", new[] { nameof(Capacity) });
            }
            if (RoomType == "Phòng hội trường" && Capacity > 100)
            {
                yield return new ValidationResult("Phòng hội trường chỉ có sức chứa dưới hoặc bằng 100 người.", new[] { nameof(Capacity) });
            }
        }
    }

    public class EditRoomViewModel : IValidatableObject
    {
        [Required]
        public int RoomId { get; set; }

        [Required(ErrorMessage = "Mã phòng không được để trống")]
        [StringLength(20, ErrorMessage = "Mã phòng không vượt quá 20 ký tự")]
        public string RoomCode { get; set; }

        [Required(ErrorMessage = "Loại phòng không được để trống")]
        [StringLength(50, ErrorMessage = "Loại phòng không vượt quá 50 ký tự")]
        public string RoomType { get; set; }

        [Required(ErrorMessage = "Vị trí không được để trống")]
        [StringLength(200, ErrorMessage = "Vị trí không vượt quá 200 ký tự")]
        public string Location { get; set; }

        [Range(1, 1000, ErrorMessage = "Sức chứa phải từ 1 đến 1000")]
        public int? Capacity { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if ((RoomType == "Phòng học (P)" || RoomType == "Phòng LAB") && Capacity > 35)
            {
                yield return new ValidationResult("Phòng học và phòng LAB chỉ được chứa tối đa 35 người.", new[] { nameof(Capacity) });
            }
            if (RoomType == "Phòng hội trường" && Capacity > 100)
            {
                yield return new ValidationResult("Phòng hội trường chỉ có sức chứa dưới hoặc bằng 100 người.", new[] { nameof(Capacity) });
            }
        }
    }

    public class RoomEquipmentsViewModel
    {
        public Room Room { get; set; } = null!;
        public List<Equipment> Equipments { get; set; } = new List<Equipment>();
    }
}
