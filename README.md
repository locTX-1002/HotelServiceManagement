# Hotel and Service Management System (HSMS)

Đồ án Software Engineering — Group 2, SE1919, FPT University.
Web app quản lý khách sạn: room map trực quan, đặt phòng, check-in/check-out, dịch vụ nhà hàng & giặt ủi, hóa đơn, báo cáo.

| Layer | Công nghệ |
|---|---|
| Frontend | React (Vite) + Tailwind CSS + Axios + React Router |
| Backend | ASP.NET Core Web API (.NET 10) — 4 project: Api / Application / Domain / Infrastructure |
| Database | SQL Server + Entity Framework Core (code-first, migrations) |
| Auth | JWT Bearer (làm ở tuần phát triển) |
| CI | GitHub Actions (`.github/workflows/`) |

## Yêu cầu môi trường

- **.NET SDK 10.0** — `dotnet --version` phải ra `10.x`
- **Node.js >= 22** — `node -v`
- **SQL Server** (Express là đủ) — instance mặc định trong repo là `.\SQLEXPRESS`
- **Git**

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
frontend/
  src/api/        # axios client (JWT interceptor có sẵn)
  src/components/ # StatusBadge, ...
  src/layouts/    # MainLayout (sidebar + topbar)
  src/pages/      # các màn hình
  src/utils/      # roomStatus.js (màu 5 trạng thái + formatVnd)
docs/             # tài liệu, quy ước route & màu
.github/workflows # CI backend + frontend
```

## Quy trình làm việc

Xem [CONTRIBUTING.md](CONTRIBUTING.md) — branch `feature/*` → PR vào `develop`, cần 1 review. Cấm push thẳng `main`/`develop`.
