using System.Collections.Generic;
using PRN_Project.Models;

namespace PRN_Project.Models.ViewModels
{
    public class DashboardViewModel
    {
        public AdminDashboardViewModel? AdminDashboard { get; set; }
        public TechnicianDashboardViewModel? TechnicianDashboard { get; set; }
        public LecturerDashboardViewModel? LecturerDashboard { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public int TotalActiveEquipments { get; set; }
        public int PendingIncidents { get; set; }
        public int UnderMaintenanceEquipments { get; set; }
        public int ActiveRooms { get; set; }
        
        public List<ActivityItem> RecentActivities { get; set; } = new List<ActivityItem>();
    }

    public class TechnicianDashboardViewModel
    {
        public int AssignedPendingIncidents { get; set; }
        public int ResolvedIncidents { get; set; }
        public int TotalTransferred { get; set; }
        public int PendingDisposals { get; set; }
        
        public List<ActivityItem> RecentActivities { get; set; } = new List<ActivityItem>();
    }

    public class LecturerDashboardViewModel
    {
        public int AssignedRooms { get; set; }
        public int TotalReportedIncidents { get; set; }
        public int PendingReportedIncidents { get; set; }
        
        public List<ActivityItem> RecentActivities { get; set; } = new List<ActivityItem>();
    }

    public class ActivityItem
    {
        public string Type { get; set; } = null!; // "Incident", "Transfer", "Disposal"
        public int? EntityId { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string TimeAgo { get; set; } = null!;
        public string IconClass { get; set; } = null!;
        public string ColorClass { get; set; } = null!;
    }
}
