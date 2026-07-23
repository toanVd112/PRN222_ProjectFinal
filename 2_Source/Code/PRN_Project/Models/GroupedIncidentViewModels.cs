using System;
using System.Collections.Generic;

namespace PRN_Project.Models
{
    public class GroupedIncidentViewModel
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; } = null!;
        public DateTime FirstReportedAt { get; set; }
        public List<IncidentReport> Incidents { get; set; } = new List<IncidentReport>();
    }

    public class GroupedIncidentsPageViewModel
    {
        public List<GroupedIncidentViewModel> GroupedIncidents { get; set; } = new List<GroupedIncidentViewModel>();
        public List<User> AvailableTechnicians { get; set; } = new List<User>();
    }
}
