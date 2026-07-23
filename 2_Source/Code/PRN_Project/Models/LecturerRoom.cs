using System;
using System.Collections.Generic;

namespace PRN_Project.Models;

public partial class LecturerRoom
{
    public int UserId { get; set; }

    public int RoomId { get; set; }

    public DateTime AssignedAt { get; set; }

    public virtual Room Room { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
