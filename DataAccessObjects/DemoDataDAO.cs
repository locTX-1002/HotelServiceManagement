using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

/// <summary>
/// Tao du lieu mau luc khoi dong de ai clone ve cung thay ngay giao dien co noi dung,
/// khong phai chay script tay. CHI chay khi database chua co khach hang nao -
/// tuc la database moi tinh, khong bao gio dung vao du lieu that.
///
/// Moc thoi gian tinh theo ngay hien tai nen luc nao cung co du: khach dang o,
/// khach den hom nay, khach qua han den, khach qua han tra.
/// </summary>
public static class DemoDataDAO
{
    public static async Task SeedAsync()
    {
        await using var context = HotelDbContextFactory.Create();

        if (await context.Guests.AnyAsync())
        {
            return;
        }

        var today = DateTime.Today;

        // Them phong cho day tang, sơ đồ nhìn không trống trải
        var roomTypes = await context.RoomTypes.OrderBy(t => t.Id).ToListAsync();
        if (roomTypes.Count == 0)
        {
            return;
        }

        var existingNumbers = await context.Rooms.Select(r => r.RoomNumber).ToListAsync();
        var extraRooms = new (string Number, int Floor, int TypeIndex)[]
        {
            ("103", 1, 0), ("104", 1, 0), ("203", 2, 1), ("302", 3, 2), ("402", 4, 3),
        };
        foreach (var (number, floor, typeIndex) in extraRooms)
        {
            if (!existingNumbers.Contains(number) && typeIndex < roomTypes.Count)
            {
                context.Rooms.Add(new Room
                {
                    RoomNumber = number,
                    Floor = floor,
                    RoomTypeId = roomTypes[typeIndex].Id,
                    Status = RoomStatus.Available,
                    IsActive = true,
                });
            }
        }
        await context.SaveChangesAsync();

        var rooms = await context.Rooms.ToDictionaryAsync(r => r.RoomNumber, r => r);
        Room? Room(string number) => rooms.TryGetValue(number, out var room) ? room : null;

        var guests = new List<Guest>
        {
            new() { FullName = "Nguyễn Minh Anh", Email = "minhanh@gmail.com", PhoneNumber = "0912345678",
                    IdentityNumber = "079201004521", Tag = GuestTag.Vip, TagNote = "Khách quen, ở trên 10 lần" },
            new() { FullName = "Trần Quốc Bảo", Email = "quocbao@gmail.com", PhoneNumber = "0987654321",
                    IdentityNumber = "079198003344" },
            new() { FullName = "Lê Thị Cẩm", Email = "lecam@gmail.com", PhoneNumber = "0903112233",
                    IdentityNumber = "079199512345" },
            new() { FullName = "Phạm Văn Dũng", PhoneNumber = "0944371151", IdentityNumber = "079200077889" },
            new() { FullName = "Hoàng Thu Hà", Email = "thuha@gmail.com", PhoneNumber = "0977889900",
                    IdentityNumber = "079202011223", Tag = GuestTag.Vip,
                    TagNote = "Công ty đối tác, ưu tiên phòng cao tầng" },
            new() { FullName = "Vũ Đình Khôi", PhoneNumber = "0966554433", IdentityNumber = "079197744556" },
            new() { FullName = "Đặng Mai Lan", Email = "mailan@gmail.com", PhoneNumber = "0933221100",
                    IdentityNumber = "079200366778" },
            new() { FullName = "Bùi Thanh Sơn", PhoneNumber = "0955443322", IdentityNumber = "079199688990",
                    Tag = GuestTag.Blacklisted, TagNote = "Từng gây ồn và làm hỏng đồ, cân nhắc trước khi nhận" },
        };
        context.Guests.AddRange(guests);
        await context.SaveChangesAsync();

        // Moi dong: phong, khach, ngay nhan lech so voi hom nay, so dem, trang thai
        var plans = new (string RoomNumber, int GuestIndex, int FromToday, int Nights,
            ReservationStatus Status, string? Note, decimal? Deposit)[]
        {
            ("201", 0, -2, 4, ReservationStatus.CheckedIn, "Xin phòng tầng cao, yên tĩnh", 500_000),
            ("101", 1, -4, 3, ReservationStatus.CheckedIn, null, null),
            ("301", 2,  0, 3, ReservationStatus.Confirmed, "Kỷ niệm ngày cưới, trang trí phòng giúp", 1_000_000),
            ("401", 3, -1, 2, ReservationStatus.Confirmed, null, null),
            ("202", 4,  2, 3, ReservationStatus.Confirmed, "Cần hoá đơn công ty", null),
            ("203", 6,  3, 3, ReservationStatus.Confirmed, null, null),
            ("102", 5,  1, 2, ReservationStatus.Pending, "Khách gọi điện giữ chỗ, chưa cọc", null),
            ("302", 2,  4, 3, ReservationStatus.Pending, null, null),
            ("103", 7, -8, 3, ReservationStatus.Completed, null, null),
            ("104", 1, -3, 1, ReservationStatus.NoShow, null, 300_000),
            ("302", 4, -6, 2, ReservationStatus.Cancelled, "Khách đổi lịch công tác", null),
        };

        var index = 1;
        var created = new List<(Reservation Reservation, ReservationStatus Status)>();
        foreach (var plan in plans)
        {
            var room = Room(plan.RoomNumber);
            if (room == null)
            {
                continue;
            }

            var checkIn = today.AddDays(plan.FromToday);
            var reservation = new Reservation
            {
                BookingCode = $"DEMO-{index:0000}",
                GuestId = guests[plan.GuestIndex].Id,
                RoomId = room.Id,
                NumberOfGuests = Math.Min(2, room.RoomType?.Capacity ?? 2),
                CheckInDate = checkIn,
                CheckOutDate = checkIn.AddDays(plan.Nights),
                Status = plan.Status,
                SpecialRequests = plan.Note,
                DepositAmount = plan.Deposit,
                DepositPaymentMethod = plan.Deposit == null ? null : PaymentMethod.Cash,
                DepositPaidAt = plan.Deposit == null ? null : checkIn.AddDays(-1),
            };
            context.Reservations.Add(reservation);
            created.Add((reservation, plan.Status));
            index++;
        }
        await context.SaveChangesAsync();

        // Khach dang o va khach da tra phong xong deu can mot luot luu tru
        foreach (var (reservation, status) in created)
        {
            if (status == ReservationStatus.CheckedIn)
            {
                context.Stays.Add(new Stay
                {
                    ReservationId = reservation.Id,
                    ActualCheckIn = reservation.CheckInDate.AddHours(14),
                    Status = StayStatus.Active,
                });
            }
            else if (status == ReservationStatus.Completed)
            {
                context.Stays.Add(new Stay
                {
                    ReservationId = reservation.Id,
                    ActualCheckIn = reservation.CheckInDate.AddHours(15),
                    ActualCheckOut = reservation.CheckOutDate.AddHours(11),
                    Status = StayStatus.Completed,
                });
            }
        }
        await context.SaveChangesAsync();

        // Mot dong phu thu mau tren luot dang o dau tien
        var firstStay = await context.Stays.FirstOrDefaultAsync(s => s.Status == StayStatus.Active);
        var surchargeItem = await context.SurchargeItems.FirstOrDefaultAsync(i => i.IsActive);
        if (firstStay != null && surchargeItem != null)
        {
            context.Surcharges.Add(new Surcharge
            {
                StayId = firstStay.Id,
                SurchargeItemId = surchargeItem.Id,
                Quantity = 1,
                UnitPriceSnapshot = surchargeItem.UnitPrice,
                Subtotal = surchargeItem.UnitPrice,
                CreatedAt = DateTime.Now,
            });
        }

        // Trang thai phong phai khop voi thuc te, khong thi so do phong noi mot dang
        // ma danh sach dat phong noi mot neo.
        foreach (var (reservation, status) in created)
        {
            var room = await context.Rooms.FirstOrDefaultAsync(r => r.Id == reservation.RoomId);
            if (room == null)
            {
                continue;
            }
            room.Status = status switch
            {
                ReservationStatus.CheckedIn => RoomStatus.Occupied,
                ReservationStatus.Confirmed => RoomStatus.Reserved,
                ReservationStatus.Completed => RoomStatus.Cleaning,
                _ => room.Status,
            };
        }
        await context.SaveChangesAsync();
    }
}
