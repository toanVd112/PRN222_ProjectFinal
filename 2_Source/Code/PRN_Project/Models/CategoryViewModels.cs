using System.ComponentModel.DataAnnotations;

namespace PRN_Project.Models
{
    public class CategoryListViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public System.DateTime CreatedAt { get; set; }
        public int EquipmentCount { get; set; }
    }

    public class CreateCategoryViewModel
    {
        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        [StringLength(150, ErrorMessage = "Tên danh mục không vượt quá 150 ký tự")]
        public string CategoryName { get; set; } = null!;
    }

    public class EditCategoryViewModel
    {
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        [StringLength(150, ErrorMessage = "Tên danh mục không vượt quá 150 ký tự")]
        public string CategoryName { get; set; } = null!;
    }
}
