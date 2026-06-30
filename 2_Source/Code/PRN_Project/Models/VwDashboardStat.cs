using System;
using System.Collections.Generic;

namespace PRN_Project.Models;

public partial class VwDashboardStat
{
    public int? TotalInUse { get; set; }

    public int? TotalPendingRepair { get; set; }

    public int? TotalUnderMaintenance { get; set; }

    public int? TotalProposedDisposal { get; set; }

    public int? TotalDisposed { get; set; }

    public int? OpenIncidents { get; set; }

    public int? OverdueIncidents { get; set; }

    public int? PendingDisposals { get; set; }
}
