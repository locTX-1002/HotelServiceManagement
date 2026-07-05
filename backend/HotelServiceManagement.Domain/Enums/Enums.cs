namespace HotelServiceManagement.Domain.Enums;

public enum RoomStatus { Available, Reserved, Occupied, Cleaning, Maintenance }

public enum ReservationStatus { Pending, Confirmed, CheckedIn, Completed, Cancelled }

public enum StayStatus { Active, Completed }

public enum ServiceOrderStatus { Pending, Processing, Completed, Cancelled }

public enum InvoiceStatus { Unpaid, PartiallyPaid, Paid, Cancelled }

public enum PaymentMethod { Cash, BankTransfer, Card }

public enum PaymentStatus { Pending, Completed, Failed }
