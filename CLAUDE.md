# CLAUDE.md - HSMS

Hướng dẫn cho AI assistant (Claude Code, Cursor...) khi làm việc trong repo này.

## Dự án

Hotel and Service Management System — đồ án Group 2 SE1919, sprint 1 tuần (05/07 → 12/07/2026). Web app quản lý khách sạn: room map, đặt phòng, check-in/out, dịch vụ, hóa đơn, báo cáo.

## Stack & lệnh chạy

- Backend: ASP.NET Core, target **net10.0** (cả nhóm cài SDK 10), EF Core 10 + SQL Server, solution `backend/HotelServiceManagement.sln`
  - Chạy: `cd backend/HotelServiceManagement.Api && dotnet run` → http://localhost:5000 (Swagger `/swagger`, health `/health`). Tự migrate + seed khi start (Development).
  - Test: `cd backend && dotnet test` (BookingRules - BR03/BR07)
  - Migration: `dotnet ef migrations add <Ten> -p HotelServiceManagement.Infrastructure -s HotelServiceManagement.Api` — CHỈ Khoa (Role 2) tạo.
- Frontend: Vite + React (JS) + Tailwind v4, thư mục `frontend/`
  - Chạy: `npm run dev` → http://localhost:5173. KHÔNG chạy `npm ci` khi dev server đang mở (lock file Windows).
- Docker (dự phòng demo): `docker compose up -d --build`

## Quy ước quan trọng

- Đọc trước: `CLEAN_ARCHITECTURE_GUIDELINES.md`, `DEVELOPMENT_WORKFLOW.md`, `docs/TeamAssignment.md` (phân công 5 role), `frontend/API_DOCS.md` (contract), `mo_ta_quan_he_erd.md`.
- Business rules BR01-BR10 theo Report 2; BR03/BR07 dùng `Application/Common/BookingRules.cs` (đã có unit test), không viết lại.
- Commit message: tiếng Việt, Conventional Commits, KHÔNG thêm trailer Co-Authored-By.
- Nhánh: `feature/*` → PR vào `develop` (cần 1 review). `main` chỉ chủ repo (Lộc) merge khi phát hành.
- FE: axios qua `src/api/client.js` (login thật trả `{accessToken, expiresAt, userId, fullName, email, role}` — lưu `accessToken`, không phải `token`), màu trạng thái qua `src/utils/roomStatus.js`, tiền qua `formatVnd`, ảnh phòng qua `src/utils/roomImages.js`. UI tiếng Việt, theme kem/espresso/terracotta + font Cormorant Garamond cho tiêu đề.
- Không commit secrets. JWT key hiện để tạm trong `appsettings.json` (giá trị placeholder, đủ dùng cho demo lớp học) — không dùng cho production thật.
- Tài khoản demo seed sẵn: `admin@hotel.com/Admin123!`, `manager@hotel.com/Manager123!`, `receptionist@hotel.com/Receptionist123!`, `service@hotel.com/Service123!`.
