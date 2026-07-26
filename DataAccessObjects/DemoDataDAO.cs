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

        // Moi buoc tu kiem tra BANG CUA CHINH NO. Truoc day ca ham chung mot chot chan
        // "da co khach thi thoat" dat o dau, nen buoc seed khuyen mai them sau nay (nam
        // cuoi ham) khong bao gio chay tren may da co du lieu - va do la ly do that su
        // khien o Khuyen mai ben man Hoa don xo ra rong tron. Tach ra thi buoc moi
        // khong con bi buoc cu chan, va sau nay them buoc nua cung the.
        await SeedPromotionsAsync(context);
        await SeedOperationsAsync(context);
        await SeedServiceMenuAsync(context);
        await SeedHistoryAsync(context);
    }

    /// <summary>
    /// Mo rong thuc don dich vu. Ban dau chi co 6 mon o 2 nhom nen man Dich vu nhin trong
    /// tron va khong the hien duoc chuyen nhom mon.
    ///
    /// Chot chan rieng cua buoc nay la DEM SO MON, khong phai "da co mon nao chua" - de
    /// may nao dang chay ban 6 mon cu van duoc bo sung.
    /// </summary>
    private static async Task SeedServiceMenuAsync(HotelDbContext context)
    {
        if (await context.ServiceItems.CountAsync() >= 16)
        {
            return;
        }

        var menu = new (string Category, string Name, decimal Price)[]
        {
            ("Nhà hàng", "Suất ăn trưa", 120_000),
            ("Nhà hàng", "Lẩu thái 2 người", 350_000),
            ("Nhà hàng", "Cà phê sữa", 35_000),
            ("Nhà hàng", "Nước cam vắt", 45_000),
            ("Minibar", "Bia lon", 30_000),
            ("Minibar", "Nước ngọt", 20_000),
            ("Minibar", "Snack khoai tây", 25_000),
            ("Minibar", "Nước suối lớn", 25_000),
            ("Spa", "Massage chân 45 phút", 250_000),
            ("Spa", "Massage toàn thân 90 phút", 450_000),
            ("Spa", "Xông hơi", 150_000),
            ("Đưa đón", "Đưa đón sân bay 1 chiều", 400_000),
            ("Đưa đón", "Thuê xe máy theo ngày", 150_000),
            ("Giặt là", "Giặt hấp vest", 90_000),
        };

        var categories = await context.ServiceCategories.ToListAsync();
        var existingItems = await context.ServiceItems.Select(x => x.ServiceName).ToListAsync();

        foreach (var (categoryName, name, price) in menu)
        {
            if (existingItems.Contains(name))
            {
                continue;
            }

            var category = categories.FirstOrDefault(c => c.CategoryName == categoryName);
            if (category == null)
            {
                category = new ServiceCategory { CategoryName = categoryName, IsActive = true };
                context.ServiceCategories.Add(category);
                await context.SaveChangesAsync();
                categories.Add(category);
            }

            context.ServiceItems.Add(new ServiceItem
            {
                ServiceCategoryId = category.Id,
                ServiceName = name,
                UnitPrice = price,
                IsAvailable = true,
            });
        }
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Lich su luu tru DA HOAN TAT trong 6 tuan gan day, kem hoa don da thu du.
    ///
    /// Khong co no thi trang Bao cao gan nhu trong: chi vai hoa don cua may hom nay, bieu do
    /// doanh thu khong ve duoc gi, va khong ai kiem tra duoc bo loc khoang ngay co chay dung
    /// khong. Day cung la du lieu de demo cong suat phong.
    ///
    /// Chot chan rieng: da co luot o nao ket thuc trước 14 ngay thi coi nhu da seed.
    /// </summary>
    private static async Task SeedHistoryAsync(HotelDbContext context)
    {
        var today = DateTime.Today;
        if (await context.Stays.AnyAsync(s => s.ActualCheckOut != null
                                              && s.ActualCheckOut < today.AddDays(-13)))
        {
            return;
        }

        var rooms = await context.Rooms.Include(r => r.RoomType).Where(r => r.IsActive).ToListAsync();
        var guests = await context.Guests.OrderBy(g => g.Id).ToListAsync();
        if (rooms.Count == 0 || guests.Count == 0)
        {
            return;
        }

        var staffId = await context.Users.Select(u => (int?)u.Id).FirstOrDefaultAsync();

        // Rai deu qua 6 tuan, so dem va phong xen ke de bieu do co len xuong that chu khong
        // phang li. Khong dung Random de may nao chay cung ra cung mot bo so.
        var plans = new (int DaysAgo, int Nights, int RoomIndex, int GuestIndex)[]
        {
            (42, 2, 0, 0), (41, 3, 2, 1), (39, 1, 4, 2), (37, 4, 1, 3),
            (35, 2, 3, 4), (33, 3, 5, 5), (31, 1, 0, 6), (29, 2, 2, 7),
            (27, 5, 6, 0), (25, 2, 1, 1), (23, 3, 4, 2), (21, 1, 3, 3),
            (19, 2, 7, 4), (17, 4, 0, 5), (15, 3, 2, 6), (14, 2, 5, 7),
        };

        var index = 1;
        foreach (var (daysAgo, nights, roomIndex, guestIndex) in plans)
        {
            var room = rooms[roomIndex % rooms.Count];
            var guest = guests[guestIndex % guests.Count];
            var checkIn = today.AddDays(-daysAgo);
            var checkOut = checkIn.AddDays(nights);

            // Khong dam vao don nao dang co cua chinh phong do
            if (await context.Reservations.AnyAsync(r => r.RoomId == room.Id
                    && r.CheckInDate < checkOut && r.CheckOutDate > checkIn))
            {
                continue;
            }

            var reservation = new Reservation
            {
                BookingCode = $"LS-{checkIn:yyMMdd}-{index:00}",
                GuestId = guest.Id,
                RoomId = room.Id,
                NumberOfGuests = Math.Min(2, room.RoomType?.Capacity ?? 2),
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                Status = ReservationStatus.Completed,
                CreatedByUserId = staffId,
            };
            context.Reservations.Add(reservation);
            await context.SaveChangesAsync();

            var stay = new Stay
            {
                ReservationId = reservation.Id,
                ActualCheckIn = checkIn.AddHours(14),
                ActualCheckOut = checkOut.AddHours(11),
                Status = StayStatus.Completed,
                CheckedInByUserId = staffId,
                CheckedOutByUserId = staffId,
            };
            context.Stays.Add(stay);
            await context.SaveChangesAsync();

            var roomCharge = nights * (room.RoomType?.BasePrice ?? 500_000m);
            // Cu ba luot thi mot luot co goi dich vu, de bao cao tach duoc doanh thu phong
            // va doanh thu dich vu chu khong phai cot dich vu bang 0 het.
            var serviceCharge = index % 3 == 0 ? 150_000m : 0m;
            var total = roomCharge + serviceCharge;

            var invoice = new Invoice
            {
                StayId = stay.Id,
                InvoiceDate = checkOut.AddHours(11),
                RoomCharge = roomCharge,
                ServiceCharge = serviceCharge,
                SurchargeAmount = 0,
                DiscountAmount = 0,
                TotalAmount = total,
                Status = InvoiceStatus.Paid,
                CreatedByUserId = staffId,
            };
            invoice.Payments.Add(new Payment
            {
                PaymentDate = checkOut.AddHours(11),
                Amount = total,
                PaymentMethod = index % 2 == 0 ? PaymentMethod.BankTransfer : PaymentMethod.Cash,
                Status = PaymentStatus.Completed,
                TransactionId = index % 2 == 0 ? $"TXN{checkIn:yyMMdd}{index:00}" : null,
                ReceivedByUserId = staffId,
            });
            context.Invoices.Add(invoice);
            await context.SaveChangesAsync();
            index++;
        }
    }

    /// <summary>
    /// Khach, phong, dat phong, luot o. Da co khach roi thi khong dung vao nua.
    /// </summary>
    private static async Task SeedOperationsAsync(HotelDbContext context)
    {
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

    /// <summary>
    /// Ma khuyen mai mau. Ngay tinh theo HOM NAY nen lan nao chay cung co du ba tinh
    /// huong de xem giao dien: dang chay, chua toi ngay, va da het han.
    /// </summary>
    private static async Task SeedPromotionsAsync(HotelDbContext context)
    {
        if (await context.Promotions.AnyAsync())
        {
            return;
        }

        var today = DateTime.Today;
        context.Promotions.AddRange(
            new Promotion
            {
                Code = "HE2026",
                Description = "Giảm 10% dịp hè cho mọi hạng phòng",
                Type = PromotionType.Percentage,
                Value = 10,
                StartDate = today.AddDays(-15),
                EndDate = today.AddDays(45),
                IsActive = true,
            },
            new Promotion
            {
                Code = "CHAOBAN",
                Description = "Giảm thẳng 200.000 đ cho khách lần đầu",
                Type = PromotionType.FixedAmount,
                Value = 200_000,
                StartDate = today.AddDays(-30),
                EndDate = today.AddDays(60),
                IsActive = true,
            },
            new Promotion
            {
                Code = "CUOITUAN",
                Description = "Giảm 5% cho đơn nhận phòng cuối tuần",
                Type = PromotionType.Percentage,
                Value = 5,
                StartDate = today.AddDays(-5),
                EndDate = today.AddDays(20),
                IsActive = true,
            },
            // Chua toi ngay bat dau - de kiem tra man hinh phan biet duoc voi "dang chay"
            new Promotion
            {
                Code = "TETMOI",
                Description = "Ưu đãi Tết - chưa tới ngày áp dụng",
                Type = PromotionType.Percentage,
                Value = 15,
                StartDate = today.AddDays(30),
                EndDate = today.AddDays(75),
                IsActive = true,
            },
            // Da het han - de kiem tra chip loc "Het han"
            new Promotion
            {
                Code = "KHAITRUONG",
                Description = "Khuyến mãi khai trương - đã kết thúc",
                Type = PromotionType.FixedAmount,
                Value = 500_000,
                StartDate = today.AddDays(-90),
                EndDate = today.AddDays(-30),
                IsActive = true,
            },
            // Da tat thu cong - khac voi het han
            new Promotion
            {
                Code = "NGUNGAP",
                Description = "Mã đã tắt, không áp dụng được nữa",
                Type = PromotionType.Percentage,
                Value = 20,
                StartDate = today.AddDays(-10),
                EndDate = today.AddDays(30),
                IsActive = false,
            });

        await context.SaveChangesAsync();
    }
}
