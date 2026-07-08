# Hướng dẫn Kiến trúc (Architecture Guidelines) - HSMS

Tài liệu này quy định tiêu chuẩn kiến trúc, cấu trúc thư mục và quy tắc phát triển cho dự án Hotel and Service Management System (.NET 10 / net10.0, cả nhóm cài cùng SDK 10). Mục tiêu: dễ bảo trì, dễ chia việc 5 người không giẫm chân nhau, hoàn thành được trong 1 tuần.

---

## 1. Tổng quan Kiến trúc

Dự án áp dụng **Clean Architecture rút gọn** (4 lớp, không MediatR/UnitOfWork — MVP 1 tuần dùng Service trực tiếp cho đơn giản). Nguyên tắc Dependency Rule: **phụ thuộc chỉ hướng vào trong.**

| Layer | Project | Trách nhiệm | Phụ thuộc vào |
| :--- | :--- | :--- | :--- |
| **Domain** | `HotelServiceManagement.Domain` | 13 Entities, Enums, BaseAuditableEntity. Không tham chiếu thư viện bên thứ ba. | Không có |
| **Application** | `HotelServiceManagement.Application` | DTOs, Interfaces (IXxxService), Services (logic nghiệp vụ), Validators, Common (BookingRules). | Domain |
| **Infrastructure** | `HotelServiceManagement.Infrastructure` | AppDbContext (EF Core), Fluent config, Migrations, DbSeeder. | Application, Domain |
| **Api** | `HotelServiceManagement.Api` | Controllers, Program.cs, Swagger, CORS, JWT config. | Application, Infrastructure |
| **Tests** | `HotelServiceManagement.Tests` | Unit test cho business rules (BookingRules - BR03, BR07). | Application |

---

## 2. Cấu trúc Thư mục

```text
HotelServiceManagement.Application/
├── Common/          # BookingRules (BR03 overlap, tính đêm/tiền) - logic thuần, có unit test
├── DTOs/            # Request/Response theo module: Auth/, Rooms/, Reservations/...
├── Interfaces/      # IAuthService, IRoomService, IReservationService...
├── Services/        # Cài đặt service, mỗi module 1 file
└── Validators/      # FluentValidation cho mọi request ghi dữ liệu

HotelServiceManagement.Domain/
├── Common/          # BaseAuditableEntity (CreatedAt, UpdatedAt)
├── Entities/        # 13 entity đúng schema Report 2 §4.4
└── Enums/           # RoomStatus, ReservationStatus, StayStatus...

HotelServiceManagement.Infrastructure/
├── Data/            # AppDbContext (Fluent config trong OnModelCreating), DbSeeder
└── Migrations/      # CHỈ Khoa (Role 2) được tạo migration
```

---

## 3. Quy tắc Đặt tên

| Thành phần | Quy tắc | Ví dụ |
| :--- | :--- | :--- |
| Entity | Danh từ số ít | `Room`, `Reservation`, `ServiceOrder` |
| Service | `[Noun]Service` + interface `I[Noun]Service` | `ReservationService`, `IReservationService` |
| DTO Request | `[Action][Noun]Request` | `CreateReservationRequest` |
| DTO Response | `[Noun]Response` | `ReservationResponse`, `RoomMapResponse` |
| Validator | `[Request]Validator` | `CreateReservationRequestValidator` |
| Controller | `[Nouns]Controller`, route kebab số nhiều | `RoomTypesController` → `/api/room-types` |

---

## 4. Quy tắc bắt buộc

1. **Controller mỏng**: không viết logic nghiệp vụ trong controller — chỉ nhận request, gọi service, trả response.
2. **Không trả entity ra ngoài**: mọi endpoint trả DTO. Entity chỉ sống từ Service xuống DB.
3. **Business rule dùng chung viết ở `Common/BookingRules`** và phải có unit test (BR03 đã có sẵn 11 test — Service chống trùng lịch PHẢI gọi `BookingRules.IsOverlapping`, không tự viết lại điều kiện).
4. **Trạng thái phòng chỉ đổi qua service**, theo đúng chuỗi: Available → Reserved → Occupied → Cleaning → Available (+ Maintenance). Không set `room.Status` từ controller.
5. **Status code đúng nghĩa**: 400 validation, 401 chưa đăng nhập, 403 sai role, 404 không tồn tại, **409 đặt phòng trùng (BR03)**.
6. **Migration**: chỉ Khoa tạo. Ai đổi entity → PR cho Khoa trước.
7. **Secrets**: JWT key dùng `dotnet user-secrets`, tuyệt đối không ghi vào appsettings commit lên repo.
