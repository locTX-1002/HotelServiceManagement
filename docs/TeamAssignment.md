# Team Assignment - Hotel and Service Management System

## 1. Tổng quan phân công

Dự án: **Hotel and Service Management System**  
Mục tiêu giai đoạn hiện tại: **Bắt đầu code MVP theo module để giảm conflict và đảm bảo demo được flow chính.**

> Lưu ý: Trong bảng hiện tại, **Role 4** và **Role 5** đều đang là **Nguyễn Minh Tú**.  
> Nếu đây là nhầm lẫn, nhóm nên đổi **Role 5** cho thành viên còn lại trước khi bắt đầu code.

---

## 2. Bảng phân công chính

| Role | Người phụ trách | Nhiệm vụ chính | Độ khó | Main Code |
|---|---|---|---|---|
| Role 1 | Phan Tiến Phát | Team Lead + Backend Auth + Stay/Check-in/out + Invoice/Payment | Khó vừa | `backend/`<br>- `AuthController`<br>- `JwtService`<br>- `UserService`<br>- `StayController`<br>- `InvoiceController`<br>- `PaymentController` |
| Role 2 | Đinh Đức Khoa | Database + EF Core + Room + Reservation + Service APIs | Khó vừa | `backend/`<br>- `AppDbContext`<br>- `Entities`<br>- `RoomTypeController`<br>- `RoomController`<br>- `GuestController`<br>- `ReservationController`<br>- `ServiceItemController`<br>- `ServiceOrderController`<br>- `ReportController` |
| Role 3 |Trương Hoàng Phúc  | Frontend layout + Login + Dashboard + Room CRUD | Vừa | `frontend/src/`<br>- `api/axiosClient.js`<br>- `routes/ProtectedRoute.jsx`<br>- `layouts/MainLayout.jsx`<br>- `pages/LoginPage.jsx`<br>- `pages/DashboardPage.jsx`<br>- `pages/RoomTypePage.jsx`<br>- `pages/RoomPage.jsx` |
| Role 4 | Trần Xuân Lộc| Frontend Room Map + Reservation + Check-in/out + Service/Invoice UI | Vừa | `frontend/src/pages/`<br>- `RoomMapPage.jsx`<br>- `ReservationPage.jsx`<br>- `CreateReservationPage.jsx`<br>- `CheckInPage.jsx`<br>- `CheckOutPage.jsx`<br>- `ServiceOrderPage.jsx`<br>- `InvoicePage.jsx` |
| Role 5 | Nguyễn Minh Tú | QA + Postman + Test cases + Seed data + Docs + Demo script + simple pages | Dễ hơn | `docs/`<br>- `TestCases.md`<br>- `UserGuide.md`<br>- `DemoScript.md`<br>- `PostmanGuide.md`<br><br>`frontend/src/pages/`<br>- `GuestListPage.jsx`<br>- `ServiceItemPage.jsx` |

---

## 3. Chi tiết từng role

## Role 1 - Phan Tiến Phát

### Vai trò

**Team Lead / Backend Core Developer**

### Nhiệm vụ chính

- Setup backend project.
- Quản lý GitHub workflow và quy tắc branch.
- Làm chức năng đăng nhập.
- Làm JWT authentication.
- Làm phân quyền theo role.
- Làm check-in/check-out.
- Làm invoice và payment.

### API phụ trách

| Method | Endpoint | Mục đích |
|---|---|---|
| POST | `/api/auth/login` | Đăng nhập và trả JWT |
| GET | `/api/auth/me` | Lấy thông tin user hiện tại |
| POST | `/api/stays/check-in` | Check-in khách |
| GET | `/api/stays/active` | Lấy danh sách stay đang hoạt động |
| POST | `/api/stays/{id}/check-out` | Check-out và tạo invoice |
| GET | `/api/invoices/{id}` | Xem chi tiết hóa đơn |
| POST | `/api/payments` | Ghi nhận thanh toán |

### File chính

```txt
backend/
- AuthController
- JwtService
- UserService
- StayController
- InvoiceController
- PaymentController
```

---

## Role 2 - Đinh Đức Khoa

### Vai trò

**Backend Developer / Database Designer**

### Nhiệm vụ chính

- Thiết kế entity.
- Tạo `AppDbContext`.
- Tạo migration.
- Seed data demo.
- Làm Room Type API.
- Làm Room API.
- Làm Guest API.
- Làm Reservation API.
- Làm Service API.
- Làm Report API.
- Xử lý logic chống đặt phòng trùng ngày.

### API phụ trách

| Method | Endpoint | Mục đích |
|---|---|---|
| GET/POST/PUT/DELETE | `/api/room-types` | Quản lý loại phòng |
| GET/POST/PUT/DELETE | `/api/rooms` | Quản lý phòng |
| GET | `/api/rooms/map` | Lấy sơ đồ phòng |
| GET/POST/PUT | `/api/guests` | Quản lý khách |
| GET/POST/PUT | `/api/reservations` | Quản lý đặt phòng |
| GET | `/api/reservations/available-rooms` | Tìm phòng trống |
| PATCH | `/api/reservations/{id}/cancel` | Hủy reservation |
| GET/POST/PUT | `/api/service-items` | Quản lý dịch vụ |
| GET/POST/PATCH | `/api/service-orders` | Quản lý order dịch vụ |
| GET | `/api/reports/dashboard` | Dashboard summary |
| GET | `/api/reports/occupancy` | Báo cáo công suất phòng |
| GET | `/api/reports/revenue` | Báo cáo doanh thu |

### File chính

```txt
backend/
- AppDbContext
- Entities
- RoomTypeController
- RoomController
- GuestController
- ReservationController
- ServiceItemController
- ServiceOrderController
- ReportController
```

### Logic quan trọng nhất

```txt
Existing.CheckInDate < NewCheckOut
AND Existing.CheckOutDate > NewCheckIn
AND Existing.RoomId = NewRoomId
AND Existing.Status IN Pending, Confirmed, CheckedIn
```

Nếu điều kiện này đúng, hệ thống phải từ chối tạo reservation mới để tránh double-booking.

---

## Role 3 - Trương Hoàng Phúc

### Vai trò

**Frontend Developer - Layout/Auth/Dashboard/Room**

### Nhiệm vụ chính

- Setup React frontend.
- Setup Axios client.
- Setup route.
- Setup protected route.
- Làm layout sidebar/topbar.
- Làm login page.
- Làm dashboard page.
- Làm room type management page.
- Làm room management page.

### Page phụ trách

| Page | Mục đích |
|---|---|
| `LoginPage.jsx` | Đăng nhập |
| `DashboardPage.jsx` | Xem tổng quan khách sạn |
| `RoomTypePage.jsx` | Quản lý loại phòng |
| `RoomPage.jsx` | Quản lý phòng |
| `MainLayout.jsx` | Layout chính |
| `ProtectedRoute.jsx` | Chặn user chưa login |

### File chính

```txt
frontend/src/
- api/axiosClient.js
- routes/ProtectedRoute.jsx
- layouts/MainLayout.jsx
- pages/LoginPage.jsx
- pages/DashboardPage.jsx
- pages/RoomTypePage.jsx
- pages/RoomPage.jsx
```

---

## Role 4 - Trần Xuân Lộc

### Vai trò

**Frontend Developer / UI Designer - Main Operation Flow**

### Nhiệm vụ chính

- Làm visual room map.
- Làm reservation UI.
- Làm available room search.
- Làm check-in UI.
- Làm check-out UI.
- Làm service order UI.
- Làm invoice UI.
- Làm payment form UI.

### Page phụ trách

| Page | Mục đích |
|---|---|
| `RoomMapPage.jsx` | Hiển thị sơ đồ phòng theo tầng và trạng thái |
| `ReservationPage.jsx` | Danh sách reservation |
| `CreateReservationPage.jsx` | Tạo reservation |
| `CheckInPage.jsx` | Check-in khách |
| `CheckOutPage.jsx` | Check-out khách |
| `ServiceOrderPage.jsx` | Thêm dịch vụ nhà hàng/giặt ủi |
| `InvoicePage.jsx` | Xem hóa đơn và thanh toán |

### File chính

```txt
frontend/src/pages/
- RoomMapPage.jsx
- ReservationPage.jsx
- CreateReservationPage.jsx
- CheckInPage.jsx
- CheckOutPage.jsx
- ServiceOrderPage.jsx
- InvoicePage.jsx
```

---

## Role 5 - Nguyễn Minh Tú

### Vai trò

**QA Tester / Documentation / Demo Support**

### Nhiệm vụ chính

- Viết test cases.
- Tạo Postman collection.
- Test API bằng Swagger/Postman.
- Chuẩn bị seed data demo.
- Viết README setup.
- Viết User Guide.
- Viết Demo Script.
- Ghi bug và báo lại cho developer.
- Làm các page đơn giản nếu còn thời gian.

### File phụ trách

```txt
docs/
- TestCases.md
- UserGuide.md
- DemoScript.md
- PostmanGuide.md

frontend/src/pages/
- GuestListPage.jsx
- ServiceItemPage.jsx
```

### Không nên giao cho Role 5

```txt
- JWT authentication
- Role authorization
- Overlapping booking validation
- Check-out invoice calculation
- EF Core migration phức tạp
```

---

## 4. Thứ tự code đề xuất

## Giai đoạn 1 - Setup nền

| Người | Công việc |
|---|---|
| Role 1 | Setup backend solution, Swagger, JWT skeleton |
| Role 2 | Tạo entity, DbContext, migration, seed data |
| Role 3 | Setup frontend layout, routing, axios |
| Role 4 | Làm room map UI bằng data giả |
| Role 5 | Viết README setup + Postman folder |

---

## Giai đoạn 2 - Room module

| Người | Công việc |
|---|---|
| Role 2 | RoomType API, Room API, RoomMap API |
| Role 3 | RoomType page, Room page |
| Role 4 | RoomMap page gọi API thật |
| Role 5 | Test Room APIs bằng Postman |

Mục tiêu:

```txt
Admin login
→ Create room type
→ Create room
→ View room map
```

---

## Giai đoạn 3 - Reservation module

| Người | Công việc |
|---|---|
| Role 2 | Guest API, Reservation API, Available Room Search |
| Role 4 | Create Reservation page |
| Role 3 | Reservation list page nếu cần |
| Role 5 | Test case đặt phòng trùng ngày |

Mục tiêu:

```txt
Receptionist search available room
→ Create reservation
→ Room status becomes Reserved
```

---

## Giai đoạn 4 - Check-in / Service / Check-out

| Người | Công việc |
|---|---|
| Role 1 | Check-in API, Check-out API, Invoice API, Payment API |
| Role 2 | Service item/order API |
| Role 4 | Check-in, Service order, Check-out UI |
| Role 5 | Test end-to-end flow |

Mục tiêu:

```txt
Reservation
→ Check-in
→ Add service
→ Check-out
→ Invoice
→ Payment
```

---

## Giai đoạn 5 - Reports + Demo

| Người | Công việc |
|---|---|
| Role 1 | Fix bug, hỗ trợ deploy backend |
| Role 2 | Report API |
| Role 3 | Dashboard/Report UI |
| Role 4 | Polish flow demo |
| Role 5 | User guide, test report, demo script |

---

## 5. Branch Git đề xuất

```txt
develop
feature/backend-auth-stay-invoice
feature/backend-room-reservation-service
feature/frontend-layout-dashboard-room
feature/frontend-roommap-reservation-checkout
feature/qa-docs-seed-postman
```

---

## 6. Checklist trước khi merge

```txt
[ ] Backend API chạy được
[ ] Swagger test được
[ ] Database lưu đúng
[ ] Frontend gọi API được
[ ] Có validation cơ bản
[ ] Có message lỗi dễ hiểu
[ ] Không hardcode dữ liệu demo trong frontend
[ ] Không commit appsettings chứa password thật
[ ] Có ít nhất 1 người khác test lại
```

---

## 7. Demo flow cần đảm bảo

```txt
Admin login
→ Create room type
→ Create room
→ Receptionist login
→ View room map
→ Search available room
→ Create reservation
→ Check-in guest
→ Service Staff login
→ Add restaurant/laundry service
→ Receptionist check-out
→ Generate invoice
→ Record payment
→ Manager login
→ View dashboard/report
```

---

## 8. Ưu tiên nếu bị trễ deadline

### Giữ lại bằng mọi giá

```txt
1. Login
2. Room management
3. Room map
4. Reservation
5. Check-in
6. Service order
7. Check-out + invoice
8. Payment
```

### Có thể làm đơn giản

```txt
Reports chỉ cần dashboard cơ bản:
- Total rooms
- Available rooms
- Occupied rooms
- Today bookings
- Total revenue
```

### Có thể bỏ nếu thiếu thời gian

```txt
- Export report
- UI animation
- Advanced filter
- Audit log
- Deploy online
- Full responsive tablet
```
