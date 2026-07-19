# Hướng dẫn chạy dự án sau đợt merge PR #17

> Áp dụng cho mọi thành viên sau khi PR #17 (`loc/fe` → `develop`, 51 commit, 118 file) đã được merge ngày 19/07/2026.

## Tóm tắt: chỉ có 3 bước

```bash
git checkout develop && git pull      # 1. lấy code mới
# 2. chạy backend một lần (F5 trong Visual Studio hoặc: dotnet run)
cd frontend && npm run dev            # 3. chạy frontend
```

Không cần gõ lệnh migration. Không cần xoá database. Không cần cài thêm thư viện frontend.

---

## 1. Lấy code mới

```bash
git checkout develop
git pull
```

Nếu bạn đang làm dở trên nhánh riêng, đồng bộ develop vào nhánh mình trước khi code tiếp:

```bash
git checkout <nhanh-cua-ban>
git merge develop
```

## 2. Backend

Mở solution rồi bấm **F5** (hoặc chạy `dotnet run` trong thư mục `backend/HotelServiceManagement.Api`).

Lúc khởi động, app **tự động** làm 2 việc:

- Tải gói NuGet mới của đợt này: `Google.Apis.Auth 1.75.0` (cần có mạng ở lần chạy đầu).
- Áp **6 migration mới** vào database trên máy bạn — xem `Program.cs:170`, ở môi trường Development app gọi sẵn `Database.Migrate()`.

Vì vậy:

- **KHÔNG** cần chạy `dotnet ef database update`.
- **KHÔNG** xoá / tạo lại database. Migration chỉ **thêm** bảng và cột, dữ liệu cũ giữ nguyên.
- Mỗi máy có database riêng (`Server=.\SQLEXPRESS`), nên **ai cũng phải tự chạy backend một lần** trên máy mình. Việc người khác đã chạy không giúp gì cho máy bạn.

Kiểm tra backend đã chạy: mở http://localhost:5000/swagger

## 3. Frontend

```bash
cd frontend
npm run dev
```

Mở http://localhost:5173

Đợt này **không thêm thư viện npm nào**, nên không bắt buộc `npm install`. Chỉ chạy `npm install` nếu máy bạn chưa từng cài `node_modules` cho dự án, hoặc gặp lỗi thiếu module.

---

## Cấu hình riêng theo máy

### Connection string (bắt buộc nếu SQL Server của bạn khác)

File `backend/HotelServiceManagement.Api/appsettings.Development.json` đang trỏ:

```
Server=.\SQLEXPRESS;Database=HotelServiceManagement;Trusted_Connection=True;...
```

Nếu máy bạn dùng instance khác (LocalDB, instance mặc định, tên khác…), sửa lại dòng này cho đúng máy mình.

> **Lưu ý:** file này được commit trong repo, nên sau khi sửa **đừng commit lên** — sẽ làm hỏng cấu hình của người khác. Nếu lỡ sửa, dùng `git checkout -- backend/HotelServiceManagement.Api/appsettings.Development.json` trước khi commit các thay đổi khác.

### File `.env` của frontend (tuỳ chọn)

Không có `.env` thì app **vẫn chạy bình thường** — `VITE_API_URL` đã có giá trị mặc định `http://localhost:5000` trong code.

Chỉ tạo `.env` khi bạn muốn hiện nút **"Đăng nhập bằng Google"**:

```bash
cd frontend
cp .env.example .env      # Windows: copy .env.example .env
```

File `.env.example` đã có sẵn Google Client ID dùng chung cho cả nhóm (ID này công khai, không phải secret — nó cũng nằm trong `appsettings.json` của repo). Nếu thiếu biến này, nút Google **tự ẩn**, không gây lỗi gì.

### Email quên mật khẩu (tuỳ chọn)

Đây là tính năng duy nhất không chạy được trên máy chưa cấu hình, vì tài khoản Gmail + App Password nằm trong user-secrets, không đi theo repo.

Hành vi khi chưa cấu hình: bấm "Quên mật khẩu" **không nổ lỗi** — backend ghi cảnh báo ra console **kèm luôn link đặt lại mật khẩu**, bạn copy link đó từ console là demo được đủ luồng.

Nếu muốn gửi email thật, chạy trong thư mục `backend/HotelServiceManagement.Api`:

```bash
dotnet user-secrets set "Smtp:FromEmail" "<gmail-cua-ban>@gmail.com"
dotnet user-secrets set "Smtp:FromPassword" "<app-password-16-ky-tu>"
```

---

## Tài khoản test

**Nhân viên** (đăng nhập tại `/login`):

| Email | Mật khẩu | Vai trò |
|---|---|---|
| admin@hotel.com | Admin123! | Admin |
| manager@hotel.com | Manager123! | Manager |
| receptionist@hotel.com | Receptionist123! | Lễ tân |
| service@hotel.com | Service123! | Nhân viên dịch vụ |

**Khách** (đăng nhập tại `/guest/dang-nhap`, đăng nhập bằng **số điện thoại**):

| Số điện thoại | Mật khẩu |
|---|---|
| 0905777888 | Guest123! |

---

## Tính năng mới của đợt này — thứ tự nên test

Luồng xương sống mới, nên chạy đúng thứ tự này để thấy toàn bộ:

1. **Khách đặt phòng online** — đăng nhập cổng khách → *Đặt phòng mới* → chọn ngày → chọn loại phòng → gửi yêu cầu. Đơn ở trạng thái **Chờ xác nhận**.
2. **Lễ tân nhận thông báo** — đăng nhập nhân viên, chuông trên thanh trên cùng có chấm đỏ, mục *"Chờ xác nhận đặt phòng"*. Bấm vào là nhảy thẳng tới trang Đặt phòng, dòng tương ứng được tô sáng.
3. **Duyệt đơn** — bấm nút **"Xác nhận"** màu xanh trên dòng đó. Đơn chuyển sang *Đã xác nhận*.
4. **Check-in** — sang trang *Check-in / Check-out*, tab *Chờ nhận phòng*, đơn vừa duyệt đã nằm ở đây → bấm Check-in.
5. **Check-out + hoá đơn** — tab *Đang ở*, khách quá giờ trả có badge đỏ **"Quá giờ"** → Check-out → tick phụ thu nếu có → tạo hoá đơn → thu tiền (thử luôn ô mã khuyến mãi).

Các tính năng khác cùng đợt: khách tự gọi dọn phòng (có phân loại yêu cầu) và gọi dịch vụ/đồ ăn khi đang lưu trú; khách xem/sửa hồ sơ + đổi mật khẩu; đăng ký bằng số điện thoại; đăng nhập Google cho cả nhân viên lẫn khách.

---

## Sự cố thường gặp

| Hiện tượng | Nguyên nhân & cách xử lý |
|---|---|
| Backend báo `Cannot open database` / `A network-related error` | Sai SQL instance. Sửa connection string trong `appsettings.Development.json` (xem mục trên). |
| Web báo "Không kết nối được máy chủ" | Backend chưa chạy. Bật backend lên rồi F5 lại trang. |
| Lỗi SQL kiểu thiếu bảng/cột (`Invalid column name ...`) | Máy bạn chưa chạy backend bản mới nên DB chưa được migrate. Chạy backend một lần là xong. |
| Không thấy nút "Đăng nhập bằng Google" | Bình thường — máy chưa có `frontend/.env`. Tạo file theo mục trên nếu cần. |
| Bấm "Quên mật khẩu" mà không nhận được email | Bình thường — chưa cấu hình SMTP. Link đặt lại nằm trong console của backend. |
| Đơn khách đặt online không thấy ở tab Chờ nhận phòng | Đúng thiết kế: đơn phải được lễ tân bấm **Xác nhận** ở trang Đặt phòng trước, sau đó mới hiện bên Check-in. |
| Nút Check-in bị mờ, không bấm được | Đơn chưa tới ngày nhận phòng (có chú thích "Nhận từ dd/MM" ngay bên cạnh). Backend cũng chặn check-in trước ngày nhận. |
| Tìm phòng trống mà không thấy phòng đang có khách ở | Đúng thiết kế (fix của đợt này): phòng đang có lượt lưu trú sẽ không được rao là trống, kể cả khi khách ở quá ngày trả dự kiến. |
