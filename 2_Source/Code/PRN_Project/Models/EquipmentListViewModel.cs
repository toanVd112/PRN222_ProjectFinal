using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PRN_Project.Models
{
    public class EquipmentListViewModel
    {
        public string? Search { get; set; }
        public int? CategoryId { get; set; }
        public int? RoomId { get; set; }
        public string? Status { get; set; }

        public List<EquipmentListItemViewModel> Equipments { get; set; } = new();

        public List<SelectListItem> Categories { get; set; } = new();
        public List<SelectListItem> Rooms { get; set; } = new();
        public List<SelectListItem> Statuses { get; set; } = new();
    }

    public class EquipmentListItemViewModel
    {
        public int EquipmentId { get; set; }
        public string AssetCode { get; set; } = null!;
        public string EquipmentName { get; set; } = null!;
        public string CategoryName { get; set; } = null!;
        public string RoomDisplayName { get; set; } = "Chưa gán";
        public string Status { get; set; } = null!;
        public DateOnly? WarrantyExpiry { get; set; }
    }
}
