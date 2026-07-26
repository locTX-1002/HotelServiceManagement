# Việc còn lại — giao cho Khoa và Tú

Cập nhật: 25/07/2026 · Nhánh `develop`

Danh sách này lấy từ đợt rà soát giao diện gần nhất. Phần Lộc đã làm xong
được đánh dấu rõ để hai bạn không làm trùng.

---

## Đã xong rồi — đừng làm lại

| Việc | Trạng thái |
|---|---|
| Validate email / SĐT / CCCD toàn app | ✅ xong (`InputPolicy`) |
| Vai trò đọc từ bảng `Roles`, bỏ ghi cứng | ✅ xong |
| Chặn tạo / gán / sửa vai trò Quản trị viên | ✅ xong |
| Ô tìm ở màn Check-in (theo phòng + tên khách) | ✅ xong |
| Kính lúp + thu nhỏ ô tìm màn Khách hàng | ✅ xong |
| Đổi chữ "Kích hoạt tài khoản" → "Cấp tài khoản đặt phòng" | ✅ xong |
| Tìm kiếm và xoá ở màn Khách hàng | ✅ **đã có sẵn từ trước** — chỉ là nút nhìn không ra |

**Luật validate đang áp dụng** — làm màn nào cũng gọi `InputPolicy`, đừng tự
viết lại:

```csharp
InputPolicy.ValidateEmail(email, required: true)      // phải có phần sau dấu chấm cuối
InputPolicy.ValidatePhone(phone, required: true)      // 10 số, bắt đầu bằng 0
InputPolicy.ValidateIdentity(cccd, required: false)   // 12 số, bắt đầu bằng 0
```

Gọi ở **cả ViewModel lẫn service**: ViewModel để người dùng thấy lỗi ngay tại
ô, service để dữ liệu xấu không vào được database dù gọi từ đâu.

---

## Gói KHOA — Module Dịch vụ

Đây là module **duy nhất còn trống** trong toàn app. Trên navbar mục "Dịch vụ"
vẫn mở ra màn placeholder.

### Đã có sẵn ở tầng dưới — chỉ cần dựng giao diện

| Có sẵn | Ở đâu |
|---|---|
| Entity `ServiceCategory`, `ServiceItem`, `ServiceOrder`, `ServiceOrderDetail` | `BusinessObjects/Entities/` |
| `IServiceCatalogService` — quản lý danh mục và món | `Services/` |
| `IServiceOrderService` — tạo đơn, đổi trạng thái | `Services/` |
| Tiền dịch vụ đã được cộng vào hoá đơn | `InvoiceService.PrepareAsync` |

Nghĩa là **không phải viết backend**, chỉ dựng màn hình và nối vào service.

### Cần làm

**1. Tab Danh mục dịch vụ**
- Danh sách nhóm dịch vụ (Nhà hàng, Giặt là) và các món trong từng nhóm
- Thêm / sửa / xoá món: tên, giá, mô tả, bật-tắt
- Dùng dialog modal + `ConfirmDialog.Ask` trước khi xoá (quy ước chung của app)

**2. Tab Gọi dịch vụ**
- Chọn một lượt khách **đang ở** → thêm món → tạo đơn
- Đổi trạng thái đơn: Chờ → Đang làm → Hoàn tất, hoặc Huỷ
- **Chỉ đơn Hoàn tất mới được tính tiền vào hoá đơn** — đơn đang mở sẽ chặn
  không cho khách trả phòng

### Nghiệp vụ phải giữ đúng

- Chỉ thêm đơn dịch vụ cho lượt lưu trú **đang hoạt động** (BR06)
- Giá món phải được **chụp lại vào dòng đơn** lúc tạo (`UnitPriceSnapshot`) —
  sau này đổi giá trong danh mục thì hoá đơn cũ không được đổi theo
- Số lượng phải lớn hơn 0
- Nhân viên dịch vụ (ServiceStaff) được thao tác ở màn này

### Mẫu để bám theo

Màn **Khuyến mãi** (`FUHotelManagementWPF/Views/Promotions/`) có cấu trúc gần
giống nhất: danh sách + dialog thêm/sửa + xác nhận xoá. Copy cách làm là nhanh
nhất, và giao diện sẽ đồng bộ với phần còn lại của app.

Đọc thêm: `docs/FRONTEND_WPF.md` (cách thêm một màn hình) và
`docs/QUY_UOC_GIAO_DIEN.md` (design token, checklist review).

---

## Gói TÚ — Kiểm thử toàn dự án

Không phải viết test tự động (đã có 89 test ở tầng service). Việc của Tú là
**kiểm bằng tay trên app đang chạy** — thứ mà test tự động không bắt được.

### Vì sao cần

Đợt rà soát vừa rồi tìm ra ba lỗi nghiệp vụ mà **cả 89 test đều không phát
hiện**, vì code chạy đúng thứ nó được viết ra để làm, chỉ là thứ đó sai:

- Khách ở quá hạn không bị tính tiền mấy đêm dôi ra
- Nút "+1 đêm" khi gia hạn cho khách quá hạn lại ra ngày trong quá khứ
- Lượt đã trả phòng mà còn nợ tiền thì biến mất khỏi màn Hoá đơn, không thu được nữa

Đó chính là loại lỗi chỉ tìm ra khi ngồi bấm thật.

### Cần làm

**1. Chạy hết luồng chính, ghi lại kết quả từng bước**

```
Đăng nhập → Sơ đồ phòng → Đặt phòng → Nhận phòng
   → Gọi dịch vụ → Ghi phụ thu → Lập hoá đơn
   → Thu tiền → Trả phòng → Xem báo cáo
```

**2. Thử các ca oái oăm** — đây mới là chỗ dễ ra lỗi:

| Ca thử | Kết quả đúng phải là |
|---|---|
| Đặt trùng phòng, trùng ngày | Bị chặn, báo rõ phòng đã có khách |
| Đặt phòng ngày trả trước ngày nhận | Bị chặn |
| Khách đặt rồi không đến | Ghi "không đến", phòng được trả về trống, báo rõ mất cọc bao nhiêu |
| Khách ở quá hạn rồi mới trả phòng | Tính đủ tiền những đêm quá hạn |
| Trả phòng khi chưa thanh toán | Bị chặn, và app phải mời sang màn Hoá đơn |
| Thu tiền nhiều hơn số còn nợ | Bị chặn |
| Thu tiền làm nhiều lần | Cộng dồn đúng, hết nợ thì hoá đơn thành Đã thanh toán |
| Còn đơn dịch vụ chưa chốt mà trả phòng | Bị chặn |
| Nhập email `abc@xyz.` | Bị chặn, báo sai định dạng |
| Nhập SĐT 9 số hoặc có chữ | Bị chặn |
| Nhập CCCD 11 số hoặc không bắt đầu bằng 0 | Bị chặn |
| Tạo tài khoản nhân viên vai trò Quản trị viên | **Không được phép** — dropdown không có Admin |
| Sửa / khoá / đổi mật khẩu tài khoản Admin | **Không được phép** — nút phải bị ẩn |
| Tự khoá tài khoản đang đăng nhập | Bị chặn |

**3. Đăng nhập đủ 4 vai trò** (Admin, Manager, Lễ tân, Nhân viên dịch vụ) và
kiểm navbar có đúng quyền không. Ví dụ: Lễ tân **không được** thấy mục Người dùng.

**4. Bấm loạn** — bấm nhanh hai lần liên tiếp vào các nút Lưu, Thanh toán,
Check-in xem có tạo trùng bản ghi không.

### Cách ghi kết quả

Mỗi lỗi tìm được ghi đủ **4 dòng** thì mới sửa được:

```
Màn hình :  Check-in / Check-out
Thao tác :  Chọn phòng 101 (đang quá hạn 2 ngày) → bấm Gia hạn → bấm "+1 đêm"
Mong đợi :  Ngày trả mới là ngày mai
Thực tế  :  Ngày trả mới là hôm qua, bấm Lưu thì báo lỗi không hiểu
```

Thiếu dòng "Thực tế" thì người sửa phải tự đoán, thường đoán sai.

### Dữ liệu để thử

App **tự tạo dữ liệu mẫu** lần chạy đầu: 11 phòng, 8 khách, 11 đơn đặt phòng
phủ đủ mọi trạng thái, với ngày tính theo hôm nay nên lúc nào cũng có sẵn một
khách đến hôm nay và một khách đang quá hạn. Không phải xin file database của ai.

Xem `docs/HUONG_DAN_CHAY.md` để cài (khoảng 5 phút).

---

## Quy tắc chung khi làm

- Làm trên nhánh riêng: `khoa/dich-vu`, `tu/kiem-thu`
- Mở pull request vào `develop`, **không tự merge** — để Lộc xem trước
- **Không tự tạo migration.** Cần thêm cột thì báo Phát (một người giữ migration)
- Trước khi mở PR: `dotnet build` không lỗi không cảnh báo, `dotnet test` xanh
- Mỗi commit một mục đích, message tiếng Việt

Có gì không rõ thì hỏi trong nhóm, đừng đoán rồi làm — sửa lại tốn thời gian
hơn nhiều so với hỏi một câu.
