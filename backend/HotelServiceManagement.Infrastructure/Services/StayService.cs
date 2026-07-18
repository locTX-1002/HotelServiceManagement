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

        public async Task<CheckOutResponse> CheckInAsync(CheckInRequest request, int checkedInByUserId)
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
                // Generic "not available" left receptionists guessing - name the actual blocker so
                // they know the next step (wait for checkout, finish cleaning, or reopen the room).
                var reason = !reservation.Room.IsActive
                    ? "the room is inactive"
                    : reservation.Room.Status switch
                    {
                        RoomStatus.Occupied => "the previous guest has not checked out yet",
                        RoomStatus.Cleaning => "housekeeping has not finished cleaning it",
                        RoomStatus.Maintenance => "it is under maintenance",
                        _ => "the room is not ready"
                    };
                return Failure($"Cannot check in room {reservation.Room.RoomNumber}: {reason}.");
            }

            var actualCheckIn = request.ActualCheckIn == default
                ? DateTime.UtcNow
                : request.ActualCheckIn;

            if (actualCheckIn >= reservation.CheckOutDate)
            {
                return Failure("Actual check-in must be before the planned check-out date.");
            }

            // Same-day early arrival is fine, but checking in days ahead of the booked range would
            // occupy a room that other reservations may legitimately hold for those dates.
            if (actualCheckIn.Date < reservation.CheckInDate.Date)
            {
                return Failure($"Cannot check in before the reservation check-in date ({reservation.CheckInDate:dd/MM/yyyy}).");
            }

            var stay = new Stay
            {
                ReservationId = reservation.Id,
                ActualCheckIn = actualCheckIn,
                CheckedInByUserId = checkedInByUserId,
                Status = StayStatus.Active
            };

            reservation.Status = ReservationStatus.CheckedIn;
            reservation.Room.Status = RoomStatus.Occupied;

            _context.Stays.Add(stay);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Reservation was cancelled/marked no-show/updated by another request between our
                // read above and this save (RowVersion mismatch) - don't check the guest in on top of that.
                return Failure("Reservation was just modified by another action. Please reload and try again.");
            }

            return new CheckOutResponse
            {
                StayId = stay.Id,
                ActualCheckOut = actualCheckIn,
                IsSuccess = true,
                Message = "Check-in successful."
            };
        }

        public async Task<CheckOutResponse> CheckOutAsync(int stayId, CheckOutRequest request, int checkedOutByUserId)
        {
            var stay = await _context.Stays
                .AsSplitQuery()
                .Include(s => s.Reservation)
                    .ThenInclude(r => r.Room)
                        .ThenInclude(r => r.RoomType)
                .Include(s => s.ServiceOrders)
                .Include(s => s.Surcharges)
                    .ThenInclude(s => s.SurchargeItem)
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

            var requestedSurcharges = request.Surcharges ?? new List<CheckOutSurchargeRequest>();
            if (requestedSurcharges.Any(s => s.SurchargeItemId <= 0 || s.Quantity <= 0))
            {
                return Failure("Each surcharge must have a valid SurchargeItemId and Quantity greater than 0.");
            }

            if (requestedSurcharges.Select(s => s.SurchargeItemId).Distinct().Count() != requestedSurcharges.Count)
            {
                return Failure("Duplicate surcharge items are not allowed.");
            }

            var requestedItemIds = requestedSurcharges.Select(s => s.SurchargeItemId).ToList();
            var surchargeItems = requestedItemIds.Count == 0
                ? new List<SurchargeItem>()
                : await _context.SurchargeItems
                    .Where(i => requestedItemIds.Contains(i.Id))
                    .ToListAsync();

            if (surchargeItems.Count != requestedItemIds.Count)
            {
                return Failure("One or more surcharge items do not exist.");
            }

            if (surchargeItems.Any(i => !i.IsActive))
            {
                return Failure("Inactive surcharge items cannot be added at check-out.");
            }

            var actualCheckOut = DateTime.UtcNow;
            var roomCharge = CalculateRoomCharge(stay, actualCheckOut);
            var serviceCharge = CalculateServiceCharge(stay);
            var itemById = surchargeItems.ToDictionary(i => i.Id);
            var surchargeLines = requestedSurcharges.Select(line =>
            {
                var item = itemById[line.SurchargeItemId];
                return new Surcharge
                {
                    StayId = stay.Id,
                    SurchargeItemId = item.Id,
                    SurchargeItem = item,
                    Quantity = line.Quantity,
                    UnitPriceSnapshot = item.UnitPrice,
                    Subtotal = item.UnitPrice * line.Quantity,
                    CreatedByUserId = checkedOutByUserId,
                    CreatedAt = actualCheckOut
                };
            }).ToList();
            var surchargeAmount = surchargeLines.Sum(s => s.Subtotal);
            var subtotal = roomCharge + serviceCharge + surchargeAmount;

            _context.Surcharges.AddRange(surchargeLines);
            foreach (var surcharge in surchargeLines)
            {
                stay.Surcharges.Add(surcharge);
            }

            stay.ActualCheckOut = actualCheckOut;
            stay.Status = StayStatus.Completed;
            stay.CheckedOutByUserId = checkedOutByUserId;
            stay.Reservation.Status = ReservationStatus.Completed;
            stay.Reservation.Room.Status = RoomStatus.Cleaning;

            if (stay.Invoice == null)
            {
                var invoice = new Invoice
                {
                    StayId = stay.Id,
                    Stay = stay,
                    InvoiceDate = actualCheckOut,
                    RoomCharge = roomCharge,
                    ServiceCharge = serviceCharge,
                    SurchargeAmount = surchargeAmount,
                    TotalAmount = subtotal,
                    CreatedByUserId = checkedOutByUserId,
                    Status = InvoiceStatus.Unpaid
                };
                _context.Invoices.Add(invoice);

                // Cọc đã thu lúc đặt phòng -> tạo Payment thật để tái dùng nguyên vẹn logic tính số dư
                // còn lại (đã hardened chống race-condition) trong ResolveInvoiceStatus/PaymentService.
                // Đây là nơi hoá đơn THẬT SỰ được tạo lần đầu (check-out tự sinh hoá đơn ngay),
                // InvoiceService.CreateInvoiceAsync chỉ là đường tạo lại thủ công khi thiếu hoá đơn.
                if (stay.Reservation.DepositAmount is > 0 and var deposit)
                {
                    invoice.Payments.Add(new Payment
                    {
                        PaymentDate = stay.Reservation.DepositPaidAt ?? actualCheckOut,
                        Amount = deposit,
                        PaymentMethod = stay.Reservation.DepositPaymentMethod ?? PaymentMethod.Cash,
                        Status = PaymentStatus.Completed,
                        TransactionId = $"DEP-{stay.Reservation.BookingCode}",
                        ReceivedByUserId = stay.Reservation.CreatedByUserId,
                    });
                }

                invoice.Status = ResolveInvoiceStatus(invoice);
            }
            else
            {
                stay.Invoice.InvoiceDate = actualCheckOut;
                stay.Invoice.RoomCharge = roomCharge;
                stay.Invoice.ServiceCharge = serviceCharge;
                stay.Invoice.SurchargeAmount = surchargeAmount;
                stay.Invoice.DiscountAmount = Math.Min(stay.Invoice.DiscountAmount, subtotal);
                stay.Invoice.TotalAmount = subtotal - stay.Invoice.DiscountAmount;
                stay.Invoice.Status = ResolveInvoiceStatus(stay.Invoice);
                stay.Invoice.CreatedByUserId ??= checkedOutByUserId;
            }

            await _context.SaveChangesAsync();

            return new CheckOutResponse
            {
                StayId = stay.Id,
                ActualCheckOut = actualCheckOut,
                TotalRoomCharges = roomCharge,
                TotalServiceCharges = serviceCharge,
                TotalSurchargeCharges = surchargeAmount,
                TotalAmount = stay.Invoice?.TotalAmount ?? subtotal,
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
