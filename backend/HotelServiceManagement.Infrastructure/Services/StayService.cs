using HotelServiceManagement.Application.DTOs.Stays;
using HotelServiceManagement.Application.Interfaces;
using HotelServiceManagement.Domain.Entities;
using HotelServiceManagement.Domain.Enums;
using HotelServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelServiceManagement.Infrastructure.Services
{
    public class StayService : IStayService
    {
        private readonly HotelDbContext _context;

        public StayService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<ActiveStayResponse>> GetActiveAsync()
        {
            var stays = await _context.Stays
                .AsNoTracking()
                .Include(s => s.Reservation)
                    .ThenInclude(r => r.Guest)
                .Include(s => s.Reservation)
                    .ThenInclude(r => r.Room)
                .Where(s => s.Status == StayStatus.Active)
                .OrderBy(s => s.ActualCheckIn)
                .ToListAsync();

            return stays.Select(s => new ActiveStayResponse
            {
                StayId = s.Id,
                ReservationId = s.ReservationId,
                BookingCode = s.Reservation.BookingCode,
                GuestName = s.Reservation.Guest.FullName,
                RoomNumber = s.Reservation.Room.RoomNumber,
                ActualCheckIn = s.ActualCheckIn,
                PlannedCheckOut = s.Reservation.CheckOutDate,
                Status = s.Status.ToString()
            }).ToList();
        }

        public async Task<CheckOutResponse> CheckInAsync(CheckInRequest request)
        {
            if (request == null || request.ReservationId <= 0)
            {
                return Failure("ReservationId is required.");
            }

            var reservation = await _context.Reservations
                .Include(r => r.Room)
                .Include(r => r.Stay)
                .FirstOrDefaultAsync(r => r.Id == request.ReservationId);

            if (reservation == null)
            {
                return Failure("Reservation not found.");
            }

            if (reservation.Status != ReservationStatus.Confirmed)
            {
                return Failure("Only confirmed reservations can be checked in.");
            }

            if (reservation.Stay != null)
            {
                return Failure("Reservation has already been checked in.");
            }

            if (!reservation.Room.IsActive ||
                reservation.Room.Status is not (RoomStatus.Available or RoomStatus.Reserved))
            {
                return Failure("Room is not available for check-in.");
            }

            var actualCheckIn = request.ActualCheckIn == default
                ? DateTime.UtcNow
                : request.ActualCheckIn;

            if (actualCheckIn >= reservation.CheckOutDate)
            {
                return Failure("Actual check-in must be before the planned check-out date.");
            }

            var stay = new Stay
            {
                ReservationId = reservation.Id,
                ActualCheckIn = actualCheckIn,
                Status = StayStatus.Active
            };

            reservation.Status = ReservationStatus.CheckedIn;
            reservation.Room.Status = RoomStatus.Occupied;

            _context.Stays.Add(stay);
            await _context.SaveChangesAsync();

            return new CheckOutResponse
            {
                StayId = stay.Id,
                ActualCheckOut = actualCheckIn,
                IsSuccess = true,
                Message = "Check-in successful."
            };
        }

        public async Task<CheckOutResponse> CheckOutAsync(int stayId)
        {
            var stay = await _context.Stays
                .AsSplitQuery()
                .Include(s => s.Reservation)
                    .ThenInclude(r => r.Room)
                        .ThenInclude(r => r.RoomType)
                .Include(s => s.ServiceOrders)
                .Include(s => s.Invoice)
                    .ThenInclude(i => i!.Payments)
                .FirstOrDefaultAsync(s => s.Id == stayId);

            if (stay == null)
            {
                return Failure("Stay not found.");
            }

            if (stay.Status != StayStatus.Active)
            {
                return Failure("Only active stays can be checked out.");
            }

            var actualCheckOut = DateTime.UtcNow;
            var roomCharge = CalculateRoomCharge(stay, actualCheckOut);
            var serviceCharge = CalculateServiceCharge(stay);
            var totalAmount = roomCharge + serviceCharge;

            stay.ActualCheckOut = actualCheckOut;
            stay.Status = StayStatus.Completed;
            stay.Reservation.Status = ReservationStatus.Completed;
            stay.Reservation.Room.Status = RoomStatus.Cleaning;

            if (stay.Invoice == null)
            {
                var invoice = new Invoice
                {
                    StayId = stay.Id,
                    InvoiceDate = actualCheckOut,
                    RoomCharge = roomCharge,
                    ServiceCharge = serviceCharge,
                    TotalAmount = totalAmount,
                    Status = InvoiceStatus.Unpaid
                };
                _context.Invoices.Add(invoice);
            }
            else
            {
                stay.Invoice.InvoiceDate = actualCheckOut;
                stay.Invoice.RoomCharge = roomCharge;
                stay.Invoice.ServiceCharge = serviceCharge;
                stay.Invoice.TotalAmount = totalAmount;
                stay.Invoice.Status = ResolveInvoiceStatus(stay.Invoice);
            }

            await _context.SaveChangesAsync();

            return new CheckOutResponse
            {
                StayId = stay.Id,
                ActualCheckOut = actualCheckOut,
                TotalRoomCharges = roomCharge,
                TotalServiceCharges = serviceCharge,
                TotalAmount = totalAmount,
                IsSuccess = true,
                Message = "Check-out successful. Invoice has been generated."
            };
        }

        private static decimal CalculateRoomCharge(Stay stay, DateTime actualCheckOut)
        {
            var nights = Math.Max(1, (actualCheckOut.Date - stay.ActualCheckIn.Date).Days);
            return nights * stay.Reservation.Room.RoomType.BasePrice;
        }

        private static decimal CalculateServiceCharge(Stay stay)
        {
            return stay.ServiceOrders
                .Where(o => o.Status != ServiceOrderStatus.Cancelled)
                .Sum(o => o.TotalAmount);
        }

        private static InvoiceStatus ResolveInvoiceStatus(Invoice invoice)
        {
            var paidAmount = invoice.Payments
                .Where(p => p.Status == PaymentStatus.Completed)
                .Sum(p => p.Amount);

            if (paidAmount <= 0)
            {
                return InvoiceStatus.Unpaid;
            }

            return paidAmount >= invoice.TotalAmount
                ? InvoiceStatus.Paid
                : InvoiceStatus.PartiallyPaid;
        }

        private static CheckOutResponse Failure(string message)
        {
            return new CheckOutResponse
            {
                IsSuccess = false,
                Message = message
            };
        }
    }
}
