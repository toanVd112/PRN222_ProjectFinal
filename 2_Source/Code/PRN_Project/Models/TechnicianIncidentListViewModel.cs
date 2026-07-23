using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PRN_Project.Models
{
    public class TechnicianIncidentListViewModel
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public int? RoomId { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public List<IncidentReport> Incidents { get; set; } = new();

        public List<SelectListItem> Rooms { get; set; } = new();
        public List<SelectListItem> Statuses { get; set; } = new();
    }
}
