# Hướng dẫn chạy dự án trên máy mình

> Dành cho thành viên nhóm 2 SE1919. Làm theo đúng thứ tự là chạy được, tổng
> cộng khoảng 5 phút.
>
> **Không ai phải xin file database.** App tự tạo database lúc chạy lần đầu.

## 1. Cần có sẵn

- Windows 10 hoặc 11
- **.NET 10 SDK**
- **SQL Server Express** — cài bản mặc định, instance tên `SQLEXPRESS`
- Visual Studio 2022 trở lên

Kiểm tra nhanh SQL Server đã chạy chưa: mở Services (gõ `services.msc`), tìm
dòng **SQL Server (SQLEXPRESS)**, trạng thái phải là Running.

## 2. Lấy code

```bash
git clone https://github.com/locTX-1002/HotelServiceManagement.git
```

```bash
git checkout develop
```

Mở file `FUHotelManagement.slnx` bằng Visual Studio. Lưu ý đuôi là **`.slnx`**
chứ không phải `.sln`.

## 3. Tạo file cấu hình riêng của máy mình

Đây là bước **bắt buộc**, bỏ qua là app không khởi động được.

Tạo file mới: `FUHotelManagementWPF/appsettings.Local.json`

```json
{
  "BootstrapAdmin": {
    "Email": "admin@hotel.com",
    "FullName": "Hotel Administrator",
    "Password": "MatKhauCuaBan@2026"
  }
}
```

File này đã được gitignore nên **không lên git**, mỗi người một bản, mật khẩu ai
nấy giữ. Đây cũng chính là mật khẩu để đăng nhập vào app.

**Mật khẩu phải đủ 4 điều kiện**, thiếu một cái là app báo lỗi ngay lúc mở:

- từ 8 ký tự trở lên
- có chữ hoa
- có chữ thường
- có chữ số và ký tự đặc biệt

Máy nào không dùng instance `SQLEXPRESS` thì thêm luôn vào file này, **đừng sửa
`appsettings.json` chung**:

```json
{
  "ConnectionStrings": {
    "FUHotelManagement": "Server=TEN_INSTANCE_CUA_BAN;Database=FUHotelManagementDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  },
  "BootstrapAdmin": {
    "Email": "admin@hotel.com",
    "FullName": "Hotel Administrator",
    "Password": "MatKhauCuaBan@2026"
  }
}
```

## 4. Chạy

Đặt **FUHotelManagementWPF** làm Startup Project rồi bấm F5.

Lần chạy đầu app sẽ hiện màn hình "Đang chuẩn bị cơ sở dữ liệu…" vài giây —
lúc đó nó đang tự tạo database `FUHotelManagementDB`, chạy migration và seed
phòng, loại phòng, bảng giá phụ thu. **Không phải chạy lệnh gì thêm.**

Đăng nhập bằng:

- Email: `admin@hotel.com`
- Mật khẩu: chính là cái vừa đặt trong `appsettings.Local.json`

Ba tài khoản `manager@`, `receptionist@`, `service@` **bị khoá tự động mỗi lần
khởi động**, đăng nhập không được. Đó là cố ý — muốn thử phân quyền thì tạo tài
khoản thật ở màn Người dùng khi màn đó làm xong.

## 5. Thêm dữ liệu mẫu để xem giao diện

Database mới chỉ có phòng trống, màn nào cũng trắng nên khó hình dung. Chạy
script này để có 9 khách, 11 đặt phòng đủ mọi trạng thái, 2 khách đang ở, 1
khách quá hạn:

```bash
sqlcmd -S .\SQLEXPRESS -d FUHotelManagementDB -E -I -f 65001 -i docs\seed-demo.sql
```

**Cờ `-f 65001` bắt buộc.** Thiếu nó thì sqlcmd đọc file UTF-8 như bảng mã cũ,
tên tiếng Việt hỏng hết thành `Nguyá»…n Minh Anh`.

Chạy lại nhiều lần vẫn an toàn — script tự kiểm tra, có rồi thì không chèn nữa.
Mốc thời gian tính theo ngày hiện tại nên chạy hôm nào cũng ra đúng tình huống.

Muốn xoá dữ liệu mẫu để nạp lại:

```bash
sqlcmd -S .\SQLEXPRESS -d FUHotelManagementDB -E -I -i docs\seed-demo-clean.sql
```

## 6. Gặp lỗi thì xem ở đây

### "Thiếu cấu hình BootstrapAdmin"

Chưa tạo `appsettings.Local.json`, hoặc mật khẩu trong đó chưa đủ mạnh. Quay lại
bước 3.

### "Không kết nối được SQL Server"

Service SQL Server chưa chạy, hoặc tên instance khác `SQLEXPRESS`. Mở
`services.msc` bật service lên, hoặc thêm `ConnectionStrings` vào file local.

### Tên tiếng Việt hiện thành ký tự lạ

Chạy seed mà quên `-f 65001`. Chạy file clean rồi nạp lại đúng lệnh ở bước 5.

### Build báo file DLL đang bị khoá

```
error MSB3027: Could not copy ... The file is locked by: FUHotelManagementWPF
```

App còn đang chạy. **Tắt app trước rồi build lại.** Lỗi này hay gặp nhất.

### Muốn chạy lệnh EF bằng tay

```bash
dotnet ef database update --project DataAccessObjects
```

**Đừng** thêm `--startup-project FUHotelManagementWPF`, nó sẽ báo thiếu
`Microsoft.EntityFrameworkCore.Design`. Không phải lỗi cấu hình: tầng WPF cố ý
không tham chiếu gói EF nào, đó là cách giữ đúng kiến trúc 3 lớp.

Thực ra **không cần chạy lệnh này bao giờ** — app tự migrate lúc khởi động.

### Muốn xoá sạch làm lại từ đầu

Xoá database rồi chạy app, nó tự dựng lại:

```bash
sqlcmd -S .\SQLEXPRESS -E -Q "DROP DATABASE FUHotelManagementDB"
```

## 7. Trước khi viết code

Đọc theo thứ tự:

1. [README.md](../README.md) — kiến trúc 3 lớp và bảng chuẩn code bắt buộc
2. [docs/PHAN_CONG.md](PHAN_CONG.md) — mình nhận gói nào, cần tạo file gì
3. [docs/FRONTEND_WPF.md](FRONTEND_WPF.md) — chuẩn viết ViewModel và View
4. [docs/QUY_UOC_GIAO_DIEN.md](QUY_UOC_GIAO_DIEN.md) — bo góc 4px, màu lấy từ token
5. [docs/NHAT_KY_GIAO_DIEN.md](NHAT_KY_GIAO_DIEN.md) — các màn đã làm và vì sao

Hai bài mẫu có sẵn để soi khi làm module mới:

- **Login** — ngắn nhất, đi qua đủ 5 tầng
- **Khách hàng** — mẫu CRUD đầy đủ nhất

Hai quy tắc dễ quên nhất:

- **Không tự chạy `dotnet ef migrations add`.** Cần thêm cột thì báo nhóm.
- **Mỗi lần build phải tắt app trước.**
