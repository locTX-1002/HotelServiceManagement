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

            var response = new DashboardReportResponse
            {
                TotalRooms = totalRooms,
                AvailableRooms = availableRooms,
                ReservedRooms = reservedRooms,
                OccupiedRooms = occupiedRooms,
                TodayBookings = todayBookings,
                ActiveStays = activeStays,
                TotalRevenue = totalRevenue
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
    }
}
