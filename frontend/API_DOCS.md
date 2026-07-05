# API Docs - Contract cho Frontend

Nguồn sự thật: Report 2 §5 + Swagger (http://localhost:5000/swagger khi backend chạy). File này để FE làm mock ĐÚNG SHAPE trước khi API xong — API xong thì Swagger là chuẩn cuối.

Base URL: `VITE_API_URL` (mặc định `http://localhost:5000`). Mọi request sau login gắn `Authorization: Bearer <token>` — `src/api/client.js` tự làm.

## Auth (Role 1 - T2)

```
POST /api/auth/login   { "email": "receptionist@hotel.com", "password": "123456" }
  200: { "token": "...", "user": { "userId": 3, "fullName": "Receptionist Demo", "role": "Receptionist" } }
  401: sai tài khoản/mật khẩu
GET  /api/auth/me      -> 200: user hiện tại (dựng menu theo role)
```

## Rooms (Role 2 - T2)

```
GET   /api/rooms/map
  200: [ { "floor": 1, "rooms": [ { "roomId": 1, "roomNumber": "101", "typeName": "Standard",
          "basePrice": 500000, "status": "Available", "guestName": null } ] } ]
GET/POST/PUT/DELETE /api/room-types, /api/rooms   (CRUD chuẩn)
PATCH /api/rooms/{id}/status   { "status": "Cleaning" }
```

`status` là chuỗi: `Available | Reserved | Occupied | Cleaning | Maintenance`.

## Guests + Reservations (T3)

```
GET  /api/guests?search=...
POST /api/guests               { fullName, phoneNumber?, email?, identityNumber?, address? }
GET  /api/reservations/available-rooms?checkIn=2026-07-10&checkOut=2026-07-12&roomType=Standard
  200: [ { "roomId": 1, "roomNumber": "101", "typeName": "Standard", "floor": 1, "basePrice": 500000 } ]
POST /api/reservations         { guest: {...} | guestId, roomId, checkInDate, checkOutDate }
  201: { "reservationId": 9, "bookingCode": "BK202607-0009", ... }
  409: trùng lịch (BR03) -> FE hiện "Phòng đã có người đặt trong khoảng ngày này"
GET  /api/reservations         danh sách (lọc theo status, ngày)
PATCH /api/reservations/{id}/cancel
```

## Stays / Service / Invoice / Payment (T3-T5)

```
POST /api/stays/check-in       { reservationId }           (BR04, BR05)
GET  /api/stays/active
POST /api/stays/{id}/check-out -> 200: invoice              (BR07, BR09)
GET  /api/service-items        (+ CRUD)
POST /api/service-orders       { stayId, items: [{ serviceItemId, quantity }] }   (BR06)
PATCH /api/service-orders/{id}/status   { "status": "Completed" }   <- BẮT BUỘC trước check-out để tính tiền dịch vụ
GET  /api/invoices/{id}
POST /api/payments             { invoiceId, amount, method: "Cash|BankTransfer|Card" }  (BR08)
```

## Reports (Role 2 - T5)

```
GET /api/reports/dashboard  -> { totalRooms, availableRooms, occupiedRooms, todayBookings, totalRevenue }
GET /api/reports/occupancy?from=&to=
GET /api/reports/revenue?from=&to=
```

## Quy ước lỗi

Response lỗi thống nhất: `{ "message": "..." }` với status code đúng nghĩa (400 validation, 401, 403, 404, 409 trùng lịch). FE hiện `message` trực tiếp cho người dùng.
