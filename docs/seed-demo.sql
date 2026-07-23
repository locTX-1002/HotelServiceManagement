-- ============================================================
-- Du lieu mau de xem giao dien voi noi dung that.
-- Chay lai nhieu lan van an toan: co kiem tra ma dat phong DEMO- truoc khi chen.
-- Moc thoi gian tinh theo NGAY HIEN TAI nen chay luc nao cung ra tinh huong dung.
--
-- Chay:  sqlcmd -S .\SQLEXPRESS -d FUHotelManagementDB -E -i docs\seed-demo.sql
-- ============================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF EXISTS (SELECT 1 FROM Reservations WHERE BookingCode LIKE 'DEMO-%')
BEGIN
    PRINT 'Da co du lieu mau roi - khong chen nua.';
    RETURN;
END

DECLARE @today date = CAST(GETDATE() AS date);

-- ---------- Them phong cho day tang ----------
INSERT INTO Rooms (RoomNumber, Floor, RoomTypeId, Status, IsActive)
SELECT v.RoomNumber, v.Floor, v.RoomTypeId, 'Available', 1
FROM (VALUES ('103',1,1), ('104',1,1), ('203',2,2), ('302',3,3), ('402',4,4)) AS v(RoomNumber, Floor, RoomTypeId)
WHERE NOT EXISTS (SELECT 1 FROM Rooms r WHERE r.RoomNumber = v.RoomNumber);

-- ---------- Khach hang ----------
INSERT INTO Guests (FullName, Email, PhoneNumber, IdentityNumber, Tag, TagNote)
VALUES
 (N'Nguyễn Minh Anh',  'minhanh@gmail.com',  '0912345678', '079201004521', 'Vip',  N'Khách quen, ở trên 10 lần'),
 (N'Trần Quốc Bảo',    'quocbao@gmail.com',  '0987654321', '079198003344', 'None', NULL),
 (N'Lê Thị Cẩm',       'lecam@gmail.com',    '0903112233', '079199512345', 'None', NULL),
 (N'Phạm Văn Dũng',    NULL,                 '0944371151', '079200077889', 'None', NULL),
 (N'Hoàng Thu Hà',     'thuha@gmail.com',    '0977889900', '079202011223', 'Vip',  N'Công ty đối tác, ưu tiên phòng cao tầng'),
 (N'Vũ Đình Khôi',     NULL,                 '0966554433', '079197744556', 'None', NULL),
 (N'Đặng Mai Lan',     'mailan@gmail.com',   '0933221100', '079200366778', 'None', NULL),
 (N'Bùi Thanh Sơn',    NULL,                 '0955443322', '079199688990', 'Blacklisted', N'Từng gây ồn và làm hỏng đồ, cân nhắc trước khi nhận');

DECLARE @g1 int = (SELECT Id FROM Guests WHERE PhoneNumber='0912345678');
DECLARE @g2 int = (SELECT Id FROM Guests WHERE PhoneNumber='0987654321');
DECLARE @g3 int = (SELECT Id FROM Guests WHERE PhoneNumber='0903112233');
DECLARE @g4 int = (SELECT Id FROM Guests WHERE PhoneNumber='0944371151');
DECLARE @g5 int = (SELECT Id FROM Guests WHERE PhoneNumber='0977889900');
DECLARE @g6 int = (SELECT Id FROM Guests WHERE PhoneNumber='0966554433');
DECLARE @g7 int = (SELECT Id FROM Guests WHERE PhoneNumber='0933221100');
DECLARE @g8 int = (SELECT Id FROM Guests WHERE PhoneNumber='0955443322');

DECLARE @r101 int = (SELECT Id FROM Rooms WHERE RoomNumber='101');
DECLARE @r102 int = (SELECT Id FROM Rooms WHERE RoomNumber='102');
DECLARE @r103 int = (SELECT Id FROM Rooms WHERE RoomNumber='103');
DECLARE @r104 int = (SELECT Id FROM Rooms WHERE RoomNumber='104');
DECLARE @r201 int = (SELECT Id FROM Rooms WHERE RoomNumber='201');
DECLARE @r202 int = (SELECT Id FROM Rooms WHERE RoomNumber='202');
DECLARE @r203 int = (SELECT Id FROM Rooms WHERE RoomNumber='203');
DECLARE @r301 int = (SELECT Id FROM Rooms WHERE RoomNumber='301');
DECLARE @r302 int = (SELECT Id FROM Rooms WHERE RoomNumber='302');
DECLARE @r401 int = (SELECT Id FROM Rooms WHERE RoomNumber='401');

-- ---------- Dat phong: du moi trang thai + du moc thoi gian ----------
INSERT INTO Reservations
 (BookingCode, GuestId, RoomId, NumberOfGuests, CheckInDate, CheckOutDate, Status, SpecialRequests, DepositAmount, DepositPaymentMethod, DepositPaidAt, CreatedByUserId)
VALUES
 -- Dang o binh thuong, tra sau 2 hom
 ('DEMO-0001', @g1, @r201, 2, DATEADD(day,-2,@today), DATEADD(day,2,@today), 'CheckedIn', N'Xin phòng tầng cao, yên tĩnh', 500000, 'Cash', DATEADD(day,-2,@today), 1),
 -- Dang o nhung DA QUA HAN TRA 1 ngay
 ('DEMO-0002', @g2, @r101, 1, DATEADD(day,-4,@today), DATEADD(day,-1,@today), 'CheckedIn', NULL, NULL, NULL, NULL, 1),
 -- Den HOM NAY, cho check-in
 ('DEMO-0003', @g3, @r301, 3, @today, DATEADD(day,3,@today), 'Confirmed', N'Kỷ niệm ngày cưới, trang trí phòng giúp', 1000000, 'BankTransfer', DATEADD(day,-3,@today), 1),
 -- Le ra den hom qua ma chua toi: QUA HAN DEN
 ('DEMO-0004', @g4, @r401, 4, DATEADD(day,-1,@today), DATEADD(day,1,@today), 'Confirmed', NULL, NULL, NULL, NULL, 1),
 -- Sap den
 ('DEMO-0005', @g5, @r202, 2, DATEADD(day,2,@today), DATEADD(day,5,@today), 'Confirmed', N'Cần hoá đơn công ty', NULL, NULL, NULL, 1),
 ('DEMO-0006', @g7, @r203, 2, DATEADD(day,3,@today), DATEADD(day,6,@today), 'Confirmed', NULL, NULL, NULL, NULL, 1),
 -- Cho xac nhan
 ('DEMO-0007', @g6, @r102, 1, DATEADD(day,1,@today), DATEADD(day,3,@today), 'Pending', N'Khách gọi điện giữ chỗ, chưa cọc', NULL, NULL, NULL, 1),
 ('DEMO-0008', @g3, @r302, 2, DATEADD(day,4,@today), DATEADD(day,7,@today), 'Pending', NULL, NULL, NULL, NULL, 1),
 -- Da tra phong xong
 ('DEMO-0009', @g8, @r103, 1, DATEADD(day,-8,@today), DATEADD(day,-5,@today), 'Completed', NULL, NULL, NULL, NULL, 1),
 -- Khach khong den
 ('DEMO-0010', @g2, @r104, 2, DATEADD(day,-3,@today), DATEADD(day,-2,@today), 'NoShow', NULL, 300000, 'Cash', DATEADD(day,-6,@today), 1),
 -- Da huy
 ('DEMO-0011', @g5, @r302, 2, DATEADD(day,-6,@today), DATEADD(day,-4,@today), 'Cancelled', N'Khách đổi lịch công tác', NULL, NULL, NULL, 1);

-- ---------- Luot luu tru ----------
INSERT INTO Stays (ReservationId, ActualCheckIn, ActualCheckOut, Status, CheckedInByUserId, CheckedOutByUserId)
SELECT Id, DATEADD(hour,14,CAST(CheckInDate AS datetime2)), NULL, 'Active', 1, NULL
FROM Reservations WHERE BookingCode IN ('DEMO-0001','DEMO-0002');

INSERT INTO Stays (ReservationId, ActualCheckIn, ActualCheckOut, Status, CheckedInByUserId, CheckedOutByUserId)
SELECT Id, DATEADD(hour,15,CAST(CheckInDate AS datetime2)), DATEADD(hour,11,CAST(CheckOutDate AS datetime2)), 'Completed', 1, 1
FROM Reservations WHERE BookingCode = 'DEMO-0009';

-- ---------- Phu thu cho luot dang o ----------
DECLARE @stay1 int = (SELECT s.Id FROM Stays s JOIN Reservations r ON r.Id=s.ReservationId WHERE r.BookingCode='DEMO-0001');
INSERT INTO Surcharges (StayId, SurchargeItemId, Quantity, UnitPriceSnapshot, Subtotal, CreatedByUserId, CreatedAt)
SELECT @stay1, i.Id, 1, i.UnitPrice, i.UnitPrice, 1, GETDATE()
FROM SurchargeItems i WHERE i.Name LIKE N'Khăn t%';

-- ---------- Trang thai phong khop voi thuc te ----------
UPDATE Rooms SET Status='Occupied' WHERE Id IN (@r201, @r101);
UPDATE Rooms SET Status='Reserved' WHERE Id IN (@r301, @r401, @r202, @r203);
UPDATE Rooms SET Status='Cleaning' WHERE Id = @r103;
UPDATE Rooms SET Status='Maintenance' WHERE Id = @r104;

PRINT 'Da tao du lieu mau.';
SELECT 'Guests' AS Bang, COUNT(*) AS SoLuong FROM Guests
UNION ALL SELECT 'Reservations', COUNT(*) FROM Reservations
UNION ALL SELECT 'Stays', COUNT(*) FROM Stays
UNION ALL SELECT 'Surcharges', COUNT(*) FROM Surcharges
UNION ALL SELECT 'Rooms', COUNT(*) FROM Rooms;
