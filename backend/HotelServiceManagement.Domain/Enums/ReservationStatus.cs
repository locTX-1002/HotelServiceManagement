namespace HotelServiceManagement.Domain.Enums
{
    public enum ReservationStatus
    {
        Pending,
        Confirmed,
        Cancelled,
        CheckedIn,
        Completed,
        // Thêm vào CUỐI - enum này serialize dạng số (chưa bật JsonStringEnumConverter),
        // chèn giữa sẽ làm lệch giá trị số của mọi trạng thái phía sau cho các client đang chạy.
        NoShow
    }
}
