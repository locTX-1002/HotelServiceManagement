namespace HotelServiceManagement.Application.DTOs.ServiceOrders;

// StayId khong nhan tu client - server tu tim stay Active cua guest dang dang nhap, giong het
// pattern HousekeepingRequestService.CreateForGuestAsync.
public class GuestCreateServiceOrderRequest
{
    public List<CreateServiceOrderDetailRequest> Items { get; set; } = new();
}
