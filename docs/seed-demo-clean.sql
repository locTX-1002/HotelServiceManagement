-- Xoa du lieu mau de nap lai. Chi dung cho ban ghi DEMO-, khong dung cham du lieu that.
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DELETE s FROM Surcharges s
JOIN Stays st ON st.Id = s.StayId
JOIN Reservations r ON r.Id = st.ReservationId
WHERE r.BookingCode LIKE 'DEMO-%';

DELETE st FROM Stays st
JOIN Reservations r ON r.Id = st.ReservationId
WHERE r.BookingCode LIKE 'DEMO-%';

DELETE FROM Reservations WHERE BookingCode LIKE 'DEMO-%';

DELETE FROM Guests WHERE PhoneNumber IN
 ('0912345678','0987654321','0903112233','0944371151',
  '0977889900','0966554433','0933221100','0955443322');

PRINT 'Da xoa du lieu mau cu.';
