namespace PRN_Project.Models;

public class RoomEquipmentViewModel
{
    public int? SelectedRoomId { get; set; }

    public string? SelectedRoomName { get; set; }

    public List<RoomOptionViewModel> ActiveRooms { get; set; } = new();

    public List<RoomEquipmentItemViewModel> Equipments { get; set; } = new();
}

public class RoomOptionViewModel
{
    public int RoomId { get; set; }

    public string RoomCode { get; set; } = null!;

    public string RoomName { get; set; } = null!;

    public string DisplayName => $"{RoomCode} - {RoomName}";
}

public class RoomEquipmentItemViewModel
{
    public int EquipmentId { get; set; }

    public string AssetCode { get; set; } = null!;

    public string EquipmentName { get; set; } = null!;

    public string CategoryName { get; set; } = null!;

    public string Status { get; set; } = null!;
}
