using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.Reports;
using HotelServiceManagement.Application.Interfaces;
using HotelServiceManagement.Domain.Enums;
using HotelServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelServiceManagement.Infrastructure.Services
{
    public class ReportService : IReportService
    {
        private readonly HotelDbContext _context;

        public ReportService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<AuthServiceResult<DashboardReportResponse>> GetDashboardAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var totalRooms = await _context.Rooms.CountAsync(r => r.IsActive);
            var availableRooms = await _context.Rooms.CountAsync(r => r.IsActive && r.Status == RoomStatus.Available);
            var reservedRooms = await _context.Rooms.CountAsync(r => r.IsActive && r.Status == RoomStatus.Reserved);
            var occupiedRooms = await _context.Rooms.CountAsync(r => r.IsActive && r.Status == RoomStatus.Occupied);
            var todayBookings = await _context.Reservations.CountAsync(r => r.CheckInDate >= today && r.CheckInDate < tomorrow);
            var activeStays = await _context.Stays.CountAsync(s => s.Status == StayStatus.Active);
            var totalRevenue = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Completed)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            var arrivalRows = await _context.Reservations
                .AsNoTracking()
                .Where(r => r.CheckInDate >= today && r.CheckInDate < tomorrow && r.Status == ReservationStatus.Confirmed)
                .OrderBy(r => r.CheckInDate)
                .Select(r => new
                {
                    r.BookingCode,
                    GuestName = r.Guest.FullName,
                    r.Room.RoomNumber,
                    r.Room.RoomType.TypeName,
                    r.CheckInDate
                })
                .ToListAsync();
            var arrivals = arrivalRows.Select(r => new ArrivalItem
            {
                BookingCode = r.BookingCode,
                GuestName = r.GuestName,
                RoomNumber = r.RoomNumber,
                TypeName = r.TypeName,
                Eta = r.CheckInDate.TimeOfDay == TimeSpan.Zero ? "14:00" : r.CheckInDate.ToString("HH:mm")
            }).ToList();

            var departureRows = await _context.Stays
                .AsNoTracking()
                .AsSplitQuery()
                .Include(s => s.Reservation).ThenInclude(r => r.Guest)
                .Include(s => s.Reservation).ThenInclude(r => r.Room).ThenInclude(r => r.RoomType)
                .Include(s => s.ServiceOrders)
                .Where(s => s.Status == StayStatus.Active &&
                            s.Reservation.CheckOutDate >= today && s.Reservation.CheckOutDate < tomorrow)
                .OrderBy(s => s.Reservation.CheckOutDate)
                .ToListAsync();
            var departures = departureRows.Select(s =>
            {
                var nights = Math.Max(1, (s.Reservation.CheckOutDate.Date - s.Reservation.CheckInDate.Date).Days);
                var roomCharge = nights * s.Reservation.Room.RoomType.BasePrice;
                var serviceCharge = s.ServiceOrders
                    .Where(o => o.Status != ServiceOrderStatus.Cancelled)
                    .Sum(o => o.TotalAmount);
                return new DepartureItem
                {
                    BookingCode = s.Reservation.BookingCode,
                    GuestName = s.Reservation.Guest.FullName,
                    RoomNumber = s.Reservation.Room.RoomNumber,
                    Nights = nights,
                    AmountDue = roomCharge + serviceCharge
                };
            }).ToList();

            var revenueFrom = today.AddDays(-6);
            var revenueRows = await _context.Payments
                .AsNoTracking()
                .Where(p => p.Status == PaymentStatus.Completed && p.PaymentDate >= revenueFrom && p.PaymentDate < tomorrow)
                .GroupBy(p => p.PaymentDate.Date)
                .Select(g => new { Date = g.Key, Amount = g.Sum(p => p.Amount) })
                .ToListAsync();
            var revenueByDate = revenueRows.ToDictionary(r => r.Date, r => r.Amount);
            var revenue7d = Enumerable.Range(0, 7)
                .Select(offset => revenueFrom.AddDays(offset))
                .Select(date => new RevenueDayItem
                {
                    Day = ToVietnameseDayLabel(date.DayOfWeek),
                    Amount = revenueByDate.GetValueOrDefault(date.Date)
                })
                .ToList();

            var response = new DashboardReportResponse
            {
                TotalRooms = totalRooms,
                AvailableRooms = availableRooms,
                ReservedRooms = reservedRooms,
                OccupiedRooms = occupiedRooms,
                TodayBookings = todayBookings,
                ActiveStays = activeStays,
                TotalRevenue = totalRevenue,
                Arrivals = arrivals,
                Departures = departures,
                Revenue7d = revenue7d,
                Alerts = Array.Empty<AlertItem>()
            };

            return AuthServiceResult<DashboardReportResponse>.Success(response);
        }

        public async Task<AuthServiceResult<OccupancyReportResponse>> GetOccupancyAsync()
        {
            var rooms = await _context.Rooms
                .Where(r => r.IsActive)
                .OrderBy(r => r.Floor)
                .ToListAsync();

            var totalRooms = rooms.Count;
            var occupiedRooms = rooms.Count(r => r.Status == RoomStatus.Occupied);
            var reservedRooms = rooms.Count(r => r.Status == RoomStatus.Reserved);

            var byFloor = rooms
                .GroupBy(r => r.Floor)
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var floorTotal = g.Count();
                    var floorOccupied = g.Count(r => r.Status == RoomStatus.Occupied);
                    var floorReserved = g.Count(r => r.Status == RoomStatus.Reserved);
                    return new OccupancyByFloorResponse
                    {
                        Floor = g.Key,
                        TotalRooms = floorTotal,
                        OccupiedRooms = floorOccupied,
                        ReservedRooms = floorReserved,
                        OccupancyRate = CalculateRate(floorOccupied + floorReserved, floorTotal)
                    };
                })
                .ToList();

            var response = new OccupancyReportResponse
            {
                TotalRooms = totalRooms,
                OccupiedRooms = occupiedRooms,
                ReservedRooms = reservedRooms,
                OccupancyRate = CalculateRate(occupiedRooms + reservedRooms, totalRooms),
                ByFloor = byFloor
            };

            return AuthServiceResult<OccupancyReportResponse>.Success(response);
        }

        public async Task<AuthServiceResult<RevenueReportResponse>> GetRevenueAsync(DateTime? fromDate, DateTime? toDate)
        {
            var from = fromDate?.Date ?? DateTime.Today.AddDays(-30);
            var to = toDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.Today.AddDays(1).AddTicks(-1);

            if (to < from)
            {
                return AuthServiceResult<RevenueReportResponse>.Failure("ToDate must be later than or equal to FromDate.");
            }

            var invoices = await _context.Invoices
                .Where(i => i.InvoiceDate >= from && i.InvoiceDate <= to)
                .ToListAsync();

            var payments = await _context.Payments
                .Where(p => p.PaymentDate >= from && p.PaymentDate <= to && p.Status == PaymentStatus.Completed)
                .ToListAsync();

            var invoicesByDay = invoices.ToLookup(i => i.InvoiceDate.Date);
            var byDay = new List<RevenueByDayResponse>();
            for (var day = from.Date; day <= to.Date; day = day.AddDays(1))
            {
                var dayInvoices = invoicesByDay[day];
                byDay.Add(new RevenueByDayResponse
                {
                    Date = day,
                    RoomRevenue = dayInvoices.Sum(i => i.RoomCharge),
                    ServiceRevenue = dayInvoices.Sum(i => i.ServiceCharge),
                    TotalRevenue = dayInvoices.Sum(i => i.TotalAmount)
                });
            }

            var response = new RevenueReportResponse
            {
                FromDate = from,
                ToDate = to,
                RoomRevenue = invoices.Sum(i => i.RoomCharge),
                ServiceRevenue = invoices.Sum(i => i.ServiceCharge),
                PaymentRevenue = payments.Sum(p => p.Amount),
                TotalRevenue = invoices.Sum(i => i.TotalAmount),
                ByDay = byDay
            };

            return AuthServiceResult<RevenueReportResponse>.Success(response);
        }

        private static decimal CalculateRate(int usedRooms, int totalRooms)
        {
            return totalRooms == 0 ? 0 : Math.Round((decimal)usedRooms / totalRooms * 100, 2);
        }

        private static string ToVietnameseDayLabel(DayOfWeek day) => day switch
        {
            DayOfWeek.Monday => "T2",
            DayOfWeek.Tuesday => "T3",
            DayOfWeek.Wednesday => "T4",
            DayOfWeek.Thursday => "T5",
            DayOfWeek.Friday => "T6",
            DayOfWeek.Saturday => "T7",
            _ => "CN"
        };
    }
}
