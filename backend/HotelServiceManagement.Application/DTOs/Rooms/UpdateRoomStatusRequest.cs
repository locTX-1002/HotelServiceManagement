using System.ComponentModel.DataAnnotations;
using HotelServiceManagement.Domain.Enums;

namespace HotelServiceManagement.Application.DTOs.Rooms;

public class UpdateRoomStatusRequest
{
    [EnumDataType(typeof(RoomStatus), ErrorMessage = "Room status is invalid.")]
    public RoomStatus Status { get; set; }
}
