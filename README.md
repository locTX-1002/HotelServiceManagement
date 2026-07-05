# Hotel and Service Management System (HSMS)

Đồ án Software Engineering — Group 2, SE1919, FPT University.
Web app quản lý khách sạn: room map trực quan, đặt phòng, check-in/check-out, dịch vụ nhà hàng & giặt ủi, hóa đơn, báo cáo.

| Layer | Công nghệ |
|---|---|
| Frontend | React (Vite) + Tailwind CSS + Axios + React Router |
| Backend | ASP.NET Core Web API (target **net8.0**, chạy được với SDK 8/9/10) — 4 project: Api / Application / Domain / Infrastructure |
| Database | SQL Server + Entity Framework Core (code-first, migrations) |
| Auth | JWT Bearer (làm ở tuần phát triển) |
| CI | GitHub Actions (`.github/workflows/`) |

## Yêu cầu môi trường

- **.NET SDK 8, 9 hoặc 10 đều được** — project target `net8.0`; máy chỉ có runtime 9/10 vẫn chạy nhờ `RollForward=LatestMajor`. Kiểm tra: `dotnet --list-sdks`
- **Node.js >= 22** — `node -v`
- **SQL Server** (Express là đủ) — instance mặc định trong repo là `.\SQLEXPRESS`
- **Git**

> ⚠️ **Windows**: clone vào đường dẫn NGẮN (vd `D:\projects\`). Đường dẫn quá dài/sâu sẽ gây lỗi `Unable to load DLL 'Microsoft.Data.SqlClient.SNI.dll'` khi backend kết nối SQL Server.

## Chạy backend (lần đầu)

```bash
cd backend
dotnet restore
# Nếu SQL Server của bạn KHÔNG phải .\SQLEXPRESS: sửa ConnectionStrings.DefaultConnection
# trong HotelServiceManagement.Api/appsettings.Development.json trước

cd HotelServiceManagement.Api
dotnet run
```

- App tự chạy migration + seed dữ liệu demo khi start (môi trường Development).
- API: http://localhost:5000 — thử http://localhost:5000/health
- Swagger: http://localhost:5000/swagger

Nếu muốn chạy migration thủ công:

```bash
cd backend
dotnet tool restore
dotnet ef database update -p HotelServiceManagement.Infrastructure -s HotelServiceManagement.Api
```

## Chạy frontend

```bash
cd frontend
npm install
cp .env.example .env   # Windows: copy .env.example .env
npm run dev
```

Mở http://localhost:5173 — trang Login hiện ra; các trang trong menu là placeholder chờ task theo Task Sheet.

## Tài khoản demo (seed sẵn)

| Email | Mật khẩu | Vai trò |
|---|---|---|
| admin@hotel.com | 123456 | Admin |
| manager@hotel.com | 123456 | Manager |
| receptionist@hotel.com | 123456 | Receptionist |
| service@hotel.com | 123456 | ServiceStaff |

## Cấu trúc thư mục

```
backend/
  HotelServiceManagement.Api/            # Controllers, Program.cs, appsettings
  HotelServiceManagement.Application/    # DTOs, Interfaces, Services, Validators
  HotelServiceManagement.Domain/         # Entities, Enums
  HotelServiceManagement.Infrastructure/ # DbContext, Migrations, Seeder
  HotelServiceManagement.Tests/          # Unit tests BookingRules (chạy: dotnet test)
frontend/
  src/api/        # axios client (JWT interceptor có sẵn)
  src/components/ # StatusBadge, ...
  src/layouts/    # MainLayout (sidebar + topbar)
  src/pages/      # các màn hình
  src/utils/      # roomStatus.js (màu 5 trạng thái + formatVnd)
docs/             # tài liệu, quy ước route & màu
.github/workflows # CI backend + frontend
```

## Chạy bằng Docker (dự phòng demo / deploy)

Cần Docker Desktop. Chạy cả SQL Server + API + Frontend trong container:

```bash
docker compose up -d --build
# API: http://localhost:5000/swagger | Frontend: http://localhost:8080
```

## Tài liệu dự án

| File | Nội dung |
|---|---|
| [CLEAN_ARCHITECTURE_GUIDELINES.md](CLEAN_ARCHITECTURE_GUIDELINES.md) | Kiến trúc 4 lớp, quy tắc đặt tên, 7 quy tắc bắt buộc |
| [DEVELOPMENT_WORKFLOW.md](DEVELOPMENT_WORKFLOW.md) | 8 bước thêm 1 tính năng từ entity đến PR |
| [docs/TeamAssignment.md](docs/TeamAssignment.md) | Phân công 5 role chính thức |
| [frontend/API_DOCS.md](frontend/API_DOCS.md) | Contract API để FE mock đúng shape |
| [mo_ta_quan_he_erd.md](mo_ta_quan_he_erd.md) | Quan hệ 13 bảng + ràng buộc DB |
| [APPLICATION_FEATURES_PLAN.md](APPLICATION_FEATURES_PLAN.md) | Trạng thái module × owner × hạn |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Nhánh, commit, PR checklist |
| [CLAUDE.md](CLAUDE.md) | Ngữ cảnh cho AI assistant |

## Quy trình làm việc

Xem [CONTRIBUTING.md](CONTRIBUTING.md) — branch `feature/*` → PR vào `develop`, cần 1 review. Cấm push thẳng `main`/`develop`.
