# Hướng dẫn luồng nghiệp vụ — FU Hotel Management

> Tài liệu này hướng dẫn sử dụng và tham chiếu nghiệp vụ cho toàn bộ hệ thống quản lý
> khách sạn FU Hotel Management (ứng dụng WPF desktop + Cổng khách hàng). Nội dung
> được viết lại từ việc đọc trực tiếp mã nguồn trên nhánh `develop` (đã bao gồm các
> tính năng vừa merge từ `feature/room-management-improvements` và `be/khoa`), không
> suy đoán. Dùng để demo, kiểm thử hoặc làm quen hệ thống.

## 1. Tổng quan

### Kiến trúc 3 lớp

```
BusinessObjects        entity + enum dùng chung
        ↓
DataAccessObjects      DAO — Singleton, câu truy vấn EF Core
        ↓
Repositories           IXxxRepository + XxxRepository — lớp trung gian
        ↓
Services               IXxxService + XxxService — QUY TẮC NGHIỆP VỤ nằm ở đây,
                        luôn là lớp kiểm tra quyền/điều kiện cuối cùng
        ↓
FUHotelManagementWPF   ViewModel (logic màn hình, MVVM) + View (.xaml)
                       Điều hướng theo kiểu ViewModel-first qua ContentControl,
                       ánh xạ ViewModel → View khai báo tại Views/ViewMappings.xaml
```

Nguyên tắc xuyên suốt cả hệ thống: **giao diện ẩn/khoá nút khi không có quyền, nhưng
tầng Service luôn tự kiểm tra lại quyền một lần nữa** — không tin bất kỳ tham số nào
gửi từ UI. Vì vậy trong tài liệu này, "vai trò nào dùng được" luôn tra theo mã quyền
(`PermissionCodes`) thật đang được service kiểm tra, không suy đoán theo tên màn hình.

### Vai trò và tài khoản

Hệ thống có **4 vai trò nhân viên**, quản lý qua bảng `Roles`/`RolePermissions` (phân
quyền động, cấu hình được qua màn "Phân quyền"):

| Vai trò | Trách nhiệm chính |
|---|---|
| Quản trị viên (Admin) | Quản lý tài khoản nhân viên, vai trò, quyền và nhật ký hệ thống |
| Quản lý (Manager) | Quản lý vận hành, danh mục nghiệp vụ, báo cáo và phê duyệt |
| Lễ tân (Receptionist) | Đặt phòng, nhận/trả phòng, khách hàng, hoá đơn và thu tiền |
| Nhân viên dịch vụ (ServiceStaff) | Xử lý đơn dịch vụ và yêu cầu buồng phòng |

Ngoài ra có **tài khoản khách** (`GuestAccount`) — không phải một vai trò trong bảng
`Roles`, chỉ dùng để đăng nhập vào Cổng khách hàng, không truy cập được bất kỳ màn
hình quản trị nội bộ nào.

Ma trận quyền chi tiết theo từng nhóm chức năng (26 dòng, đủ 4 vai trò) đã có sẵn ở
[`docs/PHAN_QUYEN_HE_THONG.md`](PHAN_QUYEN_HE_THONG.md) mục 4 — tài liệu này **không
lặp lại toàn bộ ma trận đó**, chỉ trích riêng phần liên quan ngay tại từng mục và nêu
thêm những chỗ dễ hiểu nhầm khi demo thực tế (ví dụ: theo cấu hình quyền mặc định hiện
tại, Admin **không** thấy được các module vận hành nghiệp vụ — xem bảng ở mục 2).

### Danh sách luồng nghiệp vụ (35 luồng, nhóm theo 16 mục)

| # | Luồng | Mục |
|---|---|---|
| 1 | Đăng nhập nhân viên | 2 |
| 2 | Đăng nhập khách hàng | 2 |
| 3 | Khách tự đăng ký tài khoản | 2 |
| 4 | Đăng xuất | 2 |
| 5 | Trang chủ (nội dung đổi theo vai trò) | 3 |
| 6 | Xem sơ đồ phòng / danh sách phòng | 4 |
| 7 | Quản lý phòng và loại phòng (CRUD) | 4 |
| 8 | Đưa phòng vào bảo trì / đưa ra khỏi bảo trì | 4 |
| 9 | Tạo và cập nhật đặt phòng tại quầy | 5 |
| 10 | Huỷ đặt phòng / đánh dấu Không đến | 5 |
| 11 | Tự động chuyển đơn quá hạn sang Không đến (No-show sweep) | 5 |
| 12 | Nhận phòng (check-in) | 6 |
| 13 | Gia hạn lưu trú | 6 |
| 14 | Thêm phụ thu cho lượt ở | 6 |
| 15 | Trả phòng (check-out) | 6 |
| 16 | Quản lý hồ sơ khách hàng | 7 |
| 17 | Cấp tài khoản Cổng khách hàng cho khách đã có hồ sơ | 7 |
| 18 | Quản lý danh mục dịch vụ | 8 |
| 19 | Gọi dịch vụ cho phòng đang ở (lễ tân tạo hộ) | 8 |
| 20 | Xử lý đơn dịch vụ | 8 |
| 21 | Lập / tính lại hoá đơn (kèm khuyến mãi, ưu đãi VIP, giảm giá tay) | 9 |
| 22 | Ghi nhận thanh toán | 9 |
| 23 | Huỷ hoá đơn (qua phê duyệt) | 9 |
| 24 | Huỷ giao dịch thanh toán (qua phê duyệt) | 9 |
| 25 | Quản lý chương trình khuyến mãi | 10 |
| 26 | Xem và xuất báo cáo doanh thu | 11 |
| 27 | Quản lý tài khoản nhân viên | 12 |
| 28 | Cấu hình ma trận phân quyền theo vai trò | 13 |
| 29 | Tra cứu nhật ký hệ thống | 14 |
| 30 | Xử lý hàng đợi yêu cầu — phê duyệt (5 loại yêu cầu) | 15 |
| 31 | Khách tự đặt phòng | 16 |
| 32 | Khách tự yêu cầu huỷ đặt phòng (qua phê duyệt) | 16 |
| 33 | Khách tự gọi dịch vụ trong phòng | 16 |
| 34 | Khách xem hoá đơn của mình | 16 |
| 35 | Khách xem/sửa hồ sơ cá nhân, đổi mật khẩu | 16 |

---

## 2. Đăng nhập & phân quyền

Toàn bộ đăng nhập (nhân viên lẫn khách) dùng **chung một cửa sổ, một ô nhập** —
`LoginWindow`. Hệ thống tự suy luận loại tài khoản dựa vào nội dung gõ vào:

- Chuỗi nhập **toàn chữ số** → hiểu là **số điện thoại** → thử đăng nhập khách.
- Chuỗi có chứa **`@`** → hiểu là **email nhân viên** → thử đăng nhập nhân viên.
- Không rơi vào 2 dạng trên → báo lỗi "Nhập email (nhân viên) hoặc số điện thoại
  (khách hàng)."

### Luồng 1 — Đăng nhập nhân viên

**Vai trò nào dùng**: cả 4 vai trò nhân viên.

**Mục đích**: xác thực nhân viên, nạp đúng tập quyền của vai trò vào phiên làm việc.

**Các bước thao tác**:
1. Mở ứng dụng, `LoginWindow` hiện ra.
2. Nhập email nhân viên và mật khẩu.
3. Bấm Đăng nhập (hoặc Enter). Có tuỳ chọn "Ghi nhớ đăng nhập" — nếu bật, lần sau mở
   app ô email/mật khẩu tự điền sẵn (mật khẩu được mã hoá bằng DPAPI của Windows,
   gắn với đúng máy và đúng người dùng Windows đang đăng nhập, copy file sang máy
   khác sẽ không dùng lại được).
4. Đăng nhập thành công → vào thẳng màn `Trang chủ`, sidebar chỉ hiện các module vai
   trò đó có quyền.

**Quy tắc nghiệp vụ quan trọng**:
- Tài khoản phải `IsActive = true` **và** vai trò của tài khoản cũng phải đang hoạt
  động — thiếu một trong hai điều kiện là không đăng nhập được.
- Mật khẩu xác thực bằng băm BCrypt.
- Sau đăng nhập, hệ thống nạp **tập mã quyền** (`PermissionCode`) của vai trò từ
  cơ sở dữ liệu vào phiên làm việc (`AppSession`) — mọi kiểm tra quyền trong toàn bộ
  ứng dụng đều tra theo tập mã quyền này, không so sánh tên vai trò trực tiếp.
- Không có chức năng "Quên mật khẩu" tự phục vụ cho nhân viên. Muốn đổi/đặt lại mật
  khẩu nhân viên phải nhờ Admin thực hiện ở module "Người dùng" (mục 12).

**Lưu ý / trường hợp hay nhầm**:
- Sai mật khẩu, tài khoản bị khoá, hoặc vai trò bị vô hiệu hoá đều hiện **chung một
  thông báo** "Email hoặc mật khẩu không đúng." — hệ thống cố ý không tiết lộ lý do
  cụ thể, nên nhân viên bị khoá tài khoản sẽ không tự biết mình bị khoá.
- Không có cơ chế tự khoá tài khoản sau nhiều lần đăng nhập sai liên tiếp.
- Lỗi kết nối cơ sở dữ liệu có thông báo riêng, khác với lỗi sai thông tin đăng nhập.

### Luồng 2 — Đăng nhập khách hàng

**Vai trò nào dùng**: tài khoản khách (`GuestAccount`).

**Các bước thao tác**: nhập số điện thoại đã đăng ký + mật khẩu vào đúng ô đăng nhập
chung, bấm Đăng nhập → mở cửa sổ Cổng khách hàng riêng (`GuestWindow`).

**Quy tắc nghiệp vụ quan trọng**: xác thực theo số điện thoại + mật khẩu băm BCrypt;
đăng nhập thành công có cập nhật lại `LastLoginAt`.

**Lưu ý / trường hợp hay nhầm**: sai số điện thoại hoặc sai mật khẩu đều báo chung
"Số điện thoại hoặc mật khẩu không đúng." (cùng kiểu ẩn lý do như đăng nhập nhân
viên).

### Luồng 3 — Khách tự đăng ký tài khoản

**Vai trò nào dùng**: khách vãng lai chưa có tài khoản.

**Các bước thao tác**:
1. Từ `LoginWindow`, bấm "Đăng ký" → mở cửa sổ đăng ký riêng.
2. Nhập Họ tên, Số điện thoại (bắt buộc), Email (tuỳ chọn), Mật khẩu, Xác nhận mật
   khẩu.
3. Bấm Đăng ký → nếu hợp lệ, tài khoản được tạo và **tự động đăng nhập luôn**, không
   phải quay lại màn đăng nhập gõ lại thông tin.

**Quy tắc nghiệp vụ quan trọng**:
- Số điện thoại: đúng 10 chữ số, bắt đầu bằng `0`.
- Mật khẩu: tối thiểu 8 ký tự, có chữ hoa, chữ thường, chữ số và ký tự đặc biệt.
- Định danh trùng lặp được kiểm theo **số điện thoại**, không phải email:
  - Số điện thoại đã có hồ sơ khách **và** đã có tài khoản → từ chối, yêu cầu đăng
    nhập.
  - Số điện thoại đã có **hồ sơ khách** (ví dụ lễ tân từng ghi nhận khách vãng lai)
    nhưng **chưa có tài khoản** → **từ chối tự đăng ký**, phải nhờ lễ tân xác minh và
    cấp tài khoản qua module Khách hàng (mục 7, luồng "Cấp tài khoản").
  - Số điện thoại hoàn toàn mới → tạo đồng thời hồ sơ khách và tài khoản.
- Tài khoản **kích hoạt ngay lập tức**, không có bước duyệt hay xác minh OTP.

**Lưu ý / trường hợp hay nhầm**: hiện chưa có xác minh OTP qua số điện thoại/email
khi đăng ký — đây là điểm cần biết trước khi demo phần "chống mạo danh".

### Luồng 4 — Đăng xuất

Áp dụng cho cả nhân viên và khách: bấm nút Đăng xuất → xoá toàn bộ thông tin phiên
(người dùng, quyền, hoặc thông tin khách) → quay lại `LoginWindow`.

**Lưu ý quan trọng khi demo phân quyền**: nếu Admin vừa đổi ma trận quyền của một vai
trò ở module "Phân quyền" (mục 13), **những người đang đăng nhập với vai trò đó vẫn
giữ nguyên quyền cũ trong phiên hiện tại** cho đến khi họ tự đăng xuất và đăng nhập
lại — quyền không có hiệu lực tức thời trên phiên đang mở.

### Vai trò nào thấy module nào (theo cấu hình quyền mặc định — seed)

Bảng dưới đây tổng hợp thực tế cấu hình quyền mặc định trong cơ sở dữ liệu (có thể
đổi lại qua module "Phân quyền"), rất hữu ích khi chọn vai trò để demo từng module:

| Vai trò | Module thấy trên sidebar theo cấu hình mặc định |
|---|---|
| Quản trị viên | Trang chủ (khu quản trị hệ thống), Người dùng, Phân quyền, Nhật ký hệ thống |
| Quản lý | Trang chủ (Manager Dashboard), Sơ đồ phòng (đầy đủ), Đặt phòng (chỉ xem), Dịch vụ (danh mục + xử lý đơn), Khuyến mãi, Hoá đơn (chỉ xem), Báo cáo, Phê duyệt |
| Lễ tân | Trang chủ (thao tác nhanh vận hành), Sơ đồ phòng (chỉ xem), Đặt phòng (đầy đủ), Nhận/Trả phòng, Khách hàng, Dịch vụ (tạo đơn), Hoá đơn (đầy đủ) |
| Nhân viên dịch vụ | Trang chủ, Sơ đồ phòng (chỉ xem), Dịch vụ (chỉ tab xử lý đơn) |

**Lưu ý quan trọng**: theo đúng nguyên tắc tách nhiệm vụ của dự án, **Admin theo cấu
hình mặc định không thấy bất kỳ module nghiệp vụ nào** (không có quyền xem phòng, đặt
phòng, khách hàng, dịch vụ, hoá đơn, khuyến mãi, báo cáo) — Admin chỉ quản trị định
danh và an toàn hệ thống. Nếu demo mà đăng nhập bằng Admin và không thấy module "Sơ
đồ phòng" thì đây là đúng thiết kế, không phải lỗi.

**Trạng thái liên quan**: không có enum trạng thái riêng cho đăng nhập/phân quyền
(dùng cờ `IsActive` của `User`/`Role` và tập `RolePermissions.IsAllowed`).

---

## 3. Trang chủ

**Vai trò nào dùng**: tất cả — nội dung hiển thị đổi hoàn toàn theo vai trò.

**Mục đích**: trang mở đầu sau đăng nhập, vừa là landing page giới thiệu khách sạn
vừa là bảng tin vận hành rút gọn theo vai trò.

**Các bước thao tác / nội dung theo vai trò**:
- **Mọi vai trò** đều thấy: ảnh bìa xoay vòng, lời chào theo giờ trong ngày, thanh
  tra cứu phòng trống nhanh (chọn ngày nhận/trả + số khách), lưới giới thiệu các hạng
  phòng (ảnh, sức chứa, giá từ), dải số liệu vận hành (số phòng trống, đang ở, khách
  đến hôm nay, số việc quá hạn).
- Thanh tra cứu phòng trống và nút "Đặt phòng ngay" **chỉ bấm được** với vai trò có
  quyền vận hành quầy lễ tân (Lễ tân) — vai trò khác vẫn xem được số liệu nhưng nút bị
  khoá.
- **Khu quản trị hệ thống** (chỉ hiện với Admin, hoặc vai trò có quyền quản lý tài
  khoản/xem nhật ký nhưng không có quyền vận hành quầy): số tài khoản đang hoạt động,
  số tài khoản đã khoá, số lượng theo từng vai trò, và 5 dòng nhật ký hệ thống gần
  nhất kèm lối tắt mở nhanh module Người dùng / Nhật ký hệ thống.
- **Khu Manager Dashboard** (chỉ hiện với vai trò có quyền xem báo cáo hoặc quyền
  duyệt yêu cầu, nhưng không có quyền vận hành quầy): số yêu cầu đang chờ duyệt, tỷ
  lệ lấp đầy phòng hiện tại, lối tắt mở nhanh Báo cáo / Phê duyệt / Đặt phòng / Khách
  hàng.

**Quy tắc nghiệp vụ quan trọng**: 2 khu số liệu quản trị/Manager loại trừ lẫn nhau và
loại trừ với khu vận hành quầy — một tài khoản chỉ rơi vào đúng một trong ba trạng
thái hiển thị (vận hành quầy / quản trị hệ thống / Manager), dựa theo tổ hợp quyền
đang có.

**Lưu ý / trường hợp hay nhầm**: nếu một vai trò tuỳ chỉnh vừa có quyền vận hành quầy
(ví dụ `ReservationCreate`) vừa có quyền quản trị, trang chủ sẽ ưu tiên hiển thị kiểu
"vận hành quầy" và ẩn khu quản trị/Manager — cần lưu ý khi cấu hình lại phân quyền
tuỳ biến, đừng ngạc nhiên khi khu số liệu quản trị "biến mất".

---

## 4. Sơ đồ phòng & quản lý phòng

**Vai trò nào dùng**: xem được cần quyền `room.view` (Quản lý, Lễ tân, Nhân viên
dịch vụ); sửa/xoá/đổi loại phòng cần `room.manage` (chỉ Quản lý theo cấu hình mặc
định). Lễ tân và Nhân viên dịch vụ có thêm quyền `room.maintenance.request` (gửi yêu
cầu đưa phòng vào bảo trì).

**Mục đích**: theo dõi trạng thái toàn bộ phòng theo thời gian thực và quản lý danh
mục phòng/loại phòng.

**Các bước thao tác**:
1. Mở module "Sơ đồ phòng" — có 3 tab: **Sơ đồ** (dạng lưới trực quan), **Danh sách**
   (dạng bảng), **Loại phòng**.
2. Tab Sơ đồ: phòng nhóm theo tầng, mỗi thẻ phòng có chấm màu theo trạng thái (Trống/
   Đang ở/Đã đặt/Bảo trì); có thanh thống kê đầu trang, bấm vào một con số để lọc
   nhanh theo đúng trạng thái đó; có ô tìm theo số phòng/loại phòng. **Chỉ hiện phòng
   đang hoạt động** (phòng đã ngừng dùng bị ẩn hẳn khỏi tab này).
3. Tab Danh sách: có thêm bộ lọc theo **tầng**, theo **loại phòng**, và **sắp xếp**
   theo số phòng hoặc theo giá (tăng/giảm dần). Chọn 1 dòng sẽ mở panel chi tiết bên
   phải, trong đó có **lịch sử đặt phòng gần đây** (5 lượt gần nhất) — panel này chỉ
   tải được nếu người xem có thêm quyền xem đặt phòng. Phòng đã ngừng dùng vẫn hiển
   thị trong tab này nhưng bị làm mờ và có nhãn "Ngừng dùng" — khác với tab Sơ đồ ẩn
   hẳn, đừng hiểu nhầm là số liệu 2 tab lệch nhau do lỗi.
4. Tab Loại phòng: mỗi loại phòng là 1 thẻ (tên, sức chứa, giá cơ bản, mô tả, ảnh đại
   diện theo loại — không phải theo từng phòng riêng lẻ). Bấm "Xem phòng" trên một
   thẻ sẽ tự nhảy sang tab Danh sách với bộ lọc loại phòng đã áp sẵn.
5. Thêm/sửa/xoá phòng và loại phòng qua các hộp thoại riêng (chỉ Quản lý thao tác
   được).
6. Đổi trạng thái vận hành của phòng: mở hộp thoại đổi trạng thái từ thẻ phòng.

**Quy tắc nghiệp vụ quan trọng**:
- Xoá phòng: nếu phòng đang có khách ở hoặc đang có đặt phòng đang hoạt động (chờ
  xác nhận/đã xác nhận/đã check-in) thì bị chặn hoàn toàn. Nếu phòng từng có lịch sử
  đặt (nhưng hiện không còn ràng buộc trên) thì **chuyển "Ngừng dùng" thay vì xoá
  cứng** để giữ số liệu báo cáo; chỉ xoá cứng khi phòng chưa từng được đặt bao giờ.
  Quy tắc tương tự áp dụng cho xoá loại phòng (còn phòng thuộc loại thì chuyển ngừng
  dùng thay vì xoá).
- Không cho đặt số phòng trùng.
- Không đổi tay trạng thái phòng sang "Đã đặt"/"Đang ở" — hai trạng thái này chỉ do
  luồng Đặt phòng/Nhận phòng tự gán.
- Không giảm sức chứa loại phòng xuống dưới số khách của đặt phòng đang hoạt động;
  không đổi đơn giá loại phòng khi đang có đặt phòng/lượt ở còn mở (tránh đổi giá đã
  thoả thuận với khách giữa chừng).
- **Đưa phòng vào bảo trì**: Quản lý có thể đổi trực tiếp (Trống → Bảo trì). Lễ tân/
  Nhân viên dịch vụ (chỉ có quyền yêu cầu) phải gửi **yêu cầu bảo trì** qua hàng đợi
  phê duyệt (chỉ áp dụng khi phòng đang Trống), Quản lý duyệt ở module Phê duyệt (mục
  15) mới thật sự chuyển trạng thái.
- **Đưa phòng ra khỏi bảo trì**: chỉ có thể làm **trực tiếp** bởi Quản lý — không có
  luồng yêu cầu/phê duyệt cho chiều ngược lại này. Lễ tân/Nhân viên dịch vụ nhìn thấy
  thông báo "Chỉ Quản trị viên hoặc Quản lý mới đưa phòng ra khỏi bảo trì được — nhờ
  họ mở lại giúp." mà không có nút thao tác nào.

**Trạng thái liên quan (`RoomStatus`)**:

| Giá trị | Hiển thị | Ghi chú chuyển trạng thái |
|---|---|---|
| Available | Trống | Trạng thái mặc định, có thể chuyển sang Đang dọn hoặc Bảo trì |
| Reserved | Đã đặt | Chỉ do hệ thống tự gán khi có đặt phòng chờ/đã xác nhận |
| Occupied | Đang ở | Chỉ do hệ thống tự gán khi nhận phòng (check-in) |
| Cleaning | Đang dọn | Tự gán khi trả phòng; nếu phòng đã có đặt chờ thì tự trả về Đã đặt thay vì Trống khi dọn xong |
| Maintenance | Bảo trì | Vào bằng thao tác trực tiếp (Quản lý) hoặc qua phê duyệt (Lễ tân/NV dịch vụ yêu cầu); ra chỉ bằng thao tác trực tiếp của Quản lý |

**Lưu ý / trường hợp hay nhầm**:
- Với cấu hình quyền mặc định, **Admin không thấy module này** (không có `room.view`).
- Nếu tự cấu hình lại phân quyền và gán quyền duyệt bảo trì (`room.maintenance.approve`)
  cho một vai trò không có `room.manage`, vai trò đó duyệt xong vẫn có thể bị từ chối
  ở bước thực thi vì `room.manage` mới là quyền thật sự đổi được trạng thái phòng —
  nên giữ hai quyền này đi cùng nhau khi cấu hình.
- Đổi ảnh đại diện của một loại phòng sẽ ảnh hưởng tất cả phòng thuộc loại đó, không
  phải ảnh riêng theo từng phòng.

---

## 5. Đặt phòng

**Vai trò nào dùng**: xem và tạo/cập nhật cần `reservation.view`/`reservation.create`/
`reservation.update` (theo cấu hình mặc định: Lễ tân có đủ cả 3, Quản lý chỉ có
`reservation.view`). Yêu cầu huỷ cần `reservation.cancel.request` (Lễ tân); duyệt huỷ
cần `reservation.cancel.approve` (Quản lý).

**Mục đích**: quản lý đặt phòng cho khách, gồm cả xem theo dạng danh sách và dạng
lịch tuần.

**Các bước thao tác — tạo đặt phòng tại quầy**:
1. Bấm "Tạo đặt phòng" (hoặc bấm ô trống trên tab Lịch phòng để điền sẵn ngày/phòng).
2. Tìm khách theo CCCD/số điện thoại. Có khách → khoá thông tin vào khách đó (hiện
   badge VIP/Cảnh báo nếu có). Không có khách → hệ thống tự hiện thêm form tạo khách
   mới ngay trong cùng màn hình.
3. Chọn ngày nhận/ngày trả — đổi ngày sẽ tự tìm lại danh sách phòng trống ngay lập
   tức, không cần bấm nút tìm riêng.
4. Chọn 1 phòng còn trống trong danh sách (đã lọc theo sức chứa phù hợp số khách).
5. Nhập số khách, ghi chú, tiền cọc (tuỳ chọn, kèm phương thức thanh toán nếu có cọc).
6. Chọn trạng thái ban đầu của đơn: mặc định là **Đã xác nhận** (vì chính lễ tân là
   người xác nhận tại quầy), nhưng có thể đổi tay về **Chờ xác nhận** nếu muốn giữ
   chỗ tạm.
7. Lưu — hệ thống sinh mã đặt phòng dạng `BK{ngày}{4 số}`.

**Quy tắc nghiệp vụ quan trọng**:
- Chặn đặt trùng lịch: một phòng không được có 2 đơn (Chờ xác nhận/Đã xác nhận/Đã
  check-in) giao nhau về ngày.
- Tiền cọc không được vượt quá tổng tiền phòng cả kỳ ở (số đêm × giá phòng).
- Sửa đơn chỉ áp dụng khi đơn đang Chờ xác nhận hoặc Đã xác nhận; **không đổi được
  khách** của một đơn đã tạo (chỉ đổi được phòng/ngày/số khách/ghi chú).
- **Huỷ đặt phòng luôn phải qua hàng đợi yêu cầu — phê duyệt**, bất kể đơn đang ở
  trạng thái Chờ xác nhận hay Đã xác nhận, bất kể ai là người tạo ra đơn đó: Lễ tân
  bấm Huỷ chỉ tạo ra một **yêu cầu** chờ Quản lý duyệt ở module Phê duyệt (mục 15);
  Lễ tân không có đường huỷ trực tiếp trong màn Đặt phòng.
- Đánh dấu "Không đến" (No-show) làm trực tiếp, không cần qua phê duyệt — áp dụng
  được cho cả đơn đang Chờ xác nhận, miễn đơn đó chưa từng có lượt nhận phòng.
- Đơn tạo tại quầy **không** bị chặn bởi khách thuộc danh sách Cảnh báo (chỉ hiện
  cảnh báo mềm để lễ tân tự cân nhắc) — khác với đơn khách tự đặt online (mục 16) bị
  chặn cứng.

**Tự động chuyển đơn quá hạn sang Không đến (No-show sweep)**: mỗi lần mở ứng dụng,
hệ thống tự quét và chuyển các đơn đang Chờ xác nhận/Đã xác nhận mà đã qua ngày nhận
phòng (và chưa từng có lượt nhận phòng nào) sang trạng thái **Không đến**. Đây là
việc dọn dẹp tự động của hệ thống, không phải thao tác của người dùng, không tính
theo số ngày trễ tối đa mà chỉ cần qua nửa đêm của ngày nhận là đủ điều kiện.

**Tab Lịch phòng**: xem đặt phòng dạng lịch tuần, mỗi dòng là 1 phòng, mỗi cột là 1
ngày; các đơn được vẽ thành thanh ngang kéo dài theo số đêm. Bấm vào ô trống để tạo
đơn mới điền sẵn ngày/phòng, bấm vào thanh có sẵn để mở lại đơn đó.

**Trạng thái liên quan (`ReservationStatus`)**:

| Giá trị | Hiển thị |
|---|---|
| Pending | Chờ xác nhận |
| Confirmed | Đã xác nhận |
| CheckedIn | Đã check-in |
| Completed | Hoàn tất |
| Cancelled | Đã huỷ |
| NoShow | Không đến |

**Lưu ý / trường hợp hay nhầm**:
- Trên màn Nhận/Trả phòng (mục 6), có riêng một nút "Huỷ" cho các đơn "Sắp đến" đã
  quá hạn — nút này gọi huỷ **trực tiếp** (không qua phê duyệt), khác hẳn với cách
  huỷ ở màn Đặt phòng. Với cấu hình quyền mặc định, Lễ tân nhìn thấy nút này nhưng
  bấm vào sẽ luôn báo lỗi "Bạn không có quyền huỷ đặt phòng." vì quyền huỷ thật sự
  (`reservation.cancel.approve`) chỉ Quản lý mới có — đây là điểm hay gây bối rối khi
  demo, cần biết trước để không tưởng nhầm là bug của người test.
- Khách bị đánh dấu Không đến sẽ không được hoàn cọc tự động (hệ thống chỉ cảnh báo
  trên giao diện, không tự trừ/hoàn tiền).

---

## 6. Nhận / Trả phòng

**Vai trò nào dùng**: cần quyền `stay.check_in`/`stay.check_out`/`stay.extend` — theo
cấu hình mặc định chỉ Lễ tân có đủ các quyền này.

**Mục đích**: thực hiện nhận phòng, gia hạn và trả phòng cho khách.

### Nhận phòng (check-in)

**Các bước thao tác**: từ danh sách "Sắp đến" (các đơn Đã xác nhận sắp tới ngày
nhận), chọn 1 đơn, bấm Nhận phòng.

**Quy tắc nghiệp vụ quan trọng**:
- Chỉ nhận phòng được cho đơn đang ở trạng thái **Đã xác nhận**.
- Khách phải có CCCD/hộ chiếu trong hồ sơ mới nhận phòng được.
- Không cho nhận phòng nếu đã qua ngày trả phòng ghi trên đơn.
- Nhận phòng sớm hơn ngày trên đơn: hệ thống tự kéo lùi ngày nhận trên đơn về đúng
  ngày vào thực tế. Nhận phòng muộn hơn: giữ nguyên ngày đặt (áp dụng nguyên tắc vào
  trễ không được lùi ngày tính tiền).
- Khi nhận phòng thành công: tạo lượt lưu trú (Stay) mới ở trạng thái Đang lưu trú,
  đơn đặt phòng chuyển Đã check-in, phòng chuyển Đang ở.

### Gia hạn lưu trú

**Các bước thao tác**: từ dòng khách đang ở, bấm "Gia hạn", chọn ngày trả mới (có
thể dài hơn hoặc ngắn hơn ngày hiện tại), xem tạm tính chia rõ 3 phần (đã ở theo
đơn / quá hạn / ở thêm) trước khi xác nhận.

**Quy tắc nghiệp vụ quan trọng**: chỉ gia hạn được lượt ở đang **Đang lưu trú**; ngày
trả mới phải khác ngày hiện tại và không ở quá khứ; nếu kéo dài thêm phải kiểm tra
phòng không bị đơn khác giữ chỗ trong khoảng muốn ở thêm. Thao tác này sửa trực tiếp
ngày trả trên đơn đặt phòng (không tạo bản ghi mới), giữ nguyên lịch sử và không làm
sai doanh thu.

### Thêm phụ thu

**Các bước thao tác**: mở hộp thoại Phụ thu từ dòng khách đang ở, chọn 1 mục trong
danh mục phụ thu có sẵn, nhập số lượng, bấm Thêm. Nếu mục đó đã có dòng cho lượt ở
này rồi thì **cộng dồn số lượng** vào dòng cũ thay vì tạo dòng trùng.

**Quy tắc nghiệp vụ quan trọng**: số lượng phải lớn hơn 0; đơn giá được chụp lại tại
thời điểm ghi (đổi giá trong danh mục sau đó không ảnh hưởng dòng đã ghi); không sửa/
xoá được phụ thu sau khi lượt ở đã đóng hoặc đã thanh toán.

### Trả phòng (check-out)

**Các bước thao tác**: từ dòng khách đang ở, bấm Trả phòng.

**Quy tắc nghiệp vụ quan trọng** — phải thoả tất cả các điều kiện sau:
1. Lượt ở đang ở trạng thái Đang lưu trú.
2. Không còn đơn dịch vụ nào đang **Chờ làm** cho lượt ở đó — phải hoàn tất hoặc huỷ
   hết trước.
3. Đã có hoá đơn cho lượt ở và hoá đơn đó ở trạng thái **Đã thanh toán đủ**.
4. Nếu sau khi hoá đơn đã lập mà khách phát sinh thêm dịch vụ/phụ thu mới, hệ thống
   vẫn chặn trả phòng, buộc lễ tân tính lại hoá đơn và thu nốt phần chênh lệch trước.

Nếu chưa lập/chưa thanh toán đủ hoá đơn, hệ thống hỏi và điều hướng thẳng sang màn
Hoá đơn (chọn sẵn đúng lượt ở đó) thay vì chỉ báo lỗi suông.

Khi trả phòng thành công: lượt ở chuyển Hoàn tất, đơn đặt phòng chuyển Hoàn tất,
phòng chuyển Đang dọn. Trả phòng muộn hơn ngày trên đơn thì ngày trả trên đơn được
kéo dài theo đúng giờ trả thực tế (đối xứng với luồng nhận phòng sớm).

**Trạng thái liên quan (`StayStatus`)**: Active (Đang lưu trú), Completed (Hoàn tất),
Cancelled (định nghĩa sẵn trong hệ thống nhưng hiện **chưa có bất kỳ thao tác nào**
gán được giá trị này — chưa có chức năng "huỷ một lượt lưu trú đã nhận phòng").

**Lưu ý / trường hợp hay nhầm**:
- Không có giới hạn số ngày trễ tối đa khi trả phòng muộn — trễ bao lâu vẫn tính đủ
  tiền các đêm quá hạn, hệ thống không tự huỷ lượt ở.
- Nhận phòng nhầm rồi muốn "huỷ" là chưa có chức năng — chỉ có đường đi thuận Đang
  lưu trú → Hoàn tất qua trả phòng.
- Xem thêm luồng 30 (Nhận yêu cầu buồng phòng): tầng nghiệp vụ đã có sẵn service xử
  lý yêu cầu dọn phòng/thêm khăn/thêm nước cho lượt ở đang hoạt động (4 trạng thái:
  Chờ xử lý, Đã tiếp nhận, Hoàn tất, Đã huỷ), nhưng **hiện chưa có màn hình nào** gọi
  tới service này — tính năng tồn tại ở tầng service, chưa lộ ra giao diện WPF.

---

## 7. Khách hàng

**Vai trò nào dùng**: cần `guest.view`/`guest.manage` — theo cấu hình mặc định chỉ
Lễ tân.

**Mục đích**: quản lý hồ sơ khách hàng và gắn nhãn khách đặc biệt.

**Các bước thao tác**:
1. Tìm khách theo tên/CCCD/số điện thoại — gõ là lọc ngay, không cần bấm nút.
2. Thêm/sửa hồ sơ: Họ tên, số điện thoại, CCCD, email, nhóm khách, ghi chú.
3. Gắn nhóm khách qua dropdown 3 lựa chọn: **Bình thường**, **VIP**, **Cảnh báo**
   (blacklist). Chọn VIP/Cảnh báo sẽ hiện thêm ô ghi chú lý do (không bắt buộc nhập).
4. Xoá hồ sơ khách (chỉ xoá được khách chưa từng có lịch sử đặt phòng).
5. "Cấp tài khoản" cho khách đã có hồ sơ nhưng chưa có tài khoản Cổng khách hàng —
   nhập số điện thoại (đã có sẵn) và đặt mật khẩu ban đầu cho khách.

**Quy tắc nghiệp vụ quan trọng**:
- Chặn trùng **CCCD** khi tạo/sửa hồ sơ. Hệ thống **không** chặn trùng số điện thoại
  hay email (chỉ kiểm tra đúng định dạng, không kiểm tra trùng).
- Không xoá được khách đã có lịch sử đặt phòng.
- **Cảnh báo (Blacklist) chỉ chặn khách tự đặt phòng qua Cổng khách hàng** — không
  chặn Lễ tân tạo đặt phòng hộ tại quầy cho khách đó (chỉ hiện dòng chữ cảnh báo để
  lễ tân tự cân nhắc, không có gì bị khoá).
- "Cấp tài khoản" thực chất là **tạo mới** tài khoản đăng nhập (không phải mở khoá
  tài khoản cũ); mỗi khách chỉ cấp được một lần.

**Trạng thái liên quan (`GuestTag`)**: None (Bình thường — không hiện huy hiệu), Vip
(huy hiệu VIP, được tự động cộng thêm 10% giảm giá khi lập hoá đơn — xem mục 9),
Blacklisted (huy hiệu Cảnh báo).

**Lưu ý / trường hợp hay nhầm**: đừng mặc định rằng gắn nhãn Cảnh báo sẽ "cấm" khách
đặt phòng hoàn toàn — chỉ cấm kênh tự đặt online, quầy lễ tân vẫn toàn quyền quyết
định.

---

## 8. Dịch vụ

**Vai trò nào dùng**: quản lý danh mục cần `service.catalog.manage` (Quản lý); tạo
đơn gọi dịch vụ cần `service.order.create` (Lễ tân); xử lý đơn cần
`service.order.process` (Quản lý + Nhân viên dịch vụ). Không vai trò nào vừa tạo vừa
xử lý được đơn của chính module này — tách nhiệm vụ triệt để.

**Mục đích**: quản lý danh mục dịch vụ (đồ ăn, giặt ủi, spa...) và gọi dịch vụ cho
phòng đang có khách ở.

**Các bước thao tác — Danh mục dịch vụ**:
1. Thêm nhóm dịch vụ mới (chỉ nhập tên).
2. Thêm/sửa món trong nhóm: tên, giá, trạng thái Đang bán.
3. Ngừng bán / bán lại một món — có xác nhận, ghi rõ "các đơn đã gọi trước đó vẫn giữ
   nguyên và vẫn tính tiền bình thường".

**Các bước thao tác — Gọi dịch vụ**:
1. Chọn 1 phòng đang có khách ở trong danh sách bên trái.
2. Duyệt thực đơn theo nhóm (chỉ hiện món đang bán), bấm để thêm vào giỏ — trùng món
   tự cộng dồn số lượng.
3. Bấm Tạo đơn.
4. Với mỗi đơn đã tạo, có 2 hành động: **Hoàn tất** hoặc **Huỷ** (huỷ không thể hoàn
   tác, đơn huỷ không tính vào hoá đơn).

**Quy tắc nghiệp vụ quan trọng**:
- Chỉ gọi dịch vụ được cho phòng **đang có khách lưu trú** (lượt ở còn Đang lưu trú);
  phòng trống hoặc mới đặt chưa nhận thì không gọi được — hệ thống kiểm tra lại điều
  kiện này ngay tại thời điểm tạo đơn, không chỉ dựa vào danh sách phòng hiển thị.
- **Đơn giá được chụp lại (snapshot) ngay lúc gọi** vào chi tiết đơn dịch vụ. Sau này
  đổi giá trong danh mục, đơn cũ vẫn giữ nguyên giá tại thời điểm gọi.
- Món dịch vụ **không xoá được**, chỉ ngừng bán (đổi cờ Đang bán) — giữ được lịch sử
  các đơn cũ tham chiếu tới món đó.
- Đơn dịch vụ chỉ chuyển được Chờ làm → Hoàn tất hoặc Chờ làm/Đang làm → Huỷ; Hoàn
  tất và Huỷ là hai trạng thái cuối, không đổi tiếp được nữa — nhờ vậy, đơn đã tính
  vào hoá đơn (chỉ những đơn Hoàn tất mới được cộng vào hoá đơn) sẽ không còn cách
  nào sửa/huỷ được nữa.

**Trạng thái liên quan (`ServiceOrderStatus`)**:

| Giá trị | Hiển thị |
|---|---|
| Pending | Chờ làm |
| Processing | Chờ làm *(gộp chung nhãn với Pending trên giao diện, không phân biệt)* |
| Completed | Hoàn tất |
| Cancelled | Đã huỷ |

**Lưu ý / trường hợp hay nhầm**:
- Giao diện hiện tại không có nút chuyển đơn sang "Đang làm" — gọi món xong chỉ có 2
  lựa chọn Hoàn tất hoặc Huỷ, không có bước trung gian bắt buộc.
- Nhóm danh mục dịch vụ hiện chỉ **thêm mới được**, chưa có màn hình sửa/tắt một
  nhóm đã tạo (khác với món dịch vụ bên trong, có đủ nút ngừng bán/bán lại).

---

## 9. Hoá đơn & Thanh toán

**Vai trò nào dùng**: lập/tính lại hoá đơn cần `invoice.prepare` (Lễ tân); xem hoá
đơn cần `invoice.view` (Quản lý xem, Lễ tân xem + lập); ghi nhận thanh toán cần
`payment.record` (Lễ tân). Các quyền yêu cầu/duyệt giảm giá, huỷ hoá đơn, huỷ thanh
toán chia đôi giữa Lễ tân (yêu cầu) và Quản lý (duyệt) — xem mục 15.

**Mục đích**: lập hoá đơn cho lượt ở, thu tiền nhiều lần cho tới khi đủ, xử lý giảm
giá/khuyến mãi và các thao tác huỷ nhạy cảm.

**Các bước thao tác — Lập hoá đơn**:
1. Từ danh sách lượt ở (gồm cả lượt đang ở lẫn lượt đã trả phòng nhưng còn nợ tiền),
   chọn 1 lượt, bấm "Lập hoá đơn" (nếu đã có hoá đơn thì nút đổi thành "Tính lại hoá
   đơn").
2. Xem chi tiết: tiền phòng, tiền dịch vụ, phụ thu, có thể nhập mã khuyến mãi.
3. Nếu cần giảm giá thêm ngoài khuyến mãi, nhập số tiền vào ô "Giảm giá thủ công" —
   thao tác này sẽ chuyển sang luồng yêu cầu — phê duyệt (xem bên dưới), không áp
   ngay.
4. Lưu để chốt hoá đơn.

**Công thức tính tiền**:
- Số đêm tính tiền = số ngày tròn từ giờ nhận phòng thực tế đến giờ trả phòng thực
  tế (hoặc thời điểm hiện tại nếu khách còn đang ở), **tối thiểu 1 đêm**. Khách ở bao
  nhiêu đêm thực tế thì tính tiền bấy nhiêu (không thu tối thiểu theo số đêm đã đặt).
- Tiền phòng = số đêm × giá loại phòng.
- Tiền dịch vụ = tổng các đơn dịch vụ đã **Hoàn tất** của lượt ở đó (đơn chưa hoàn
  tất không tính).
- Phụ thu = tổng toàn bộ các dòng phụ thu đã ghi cho lượt ở, luôn cộng vào tổng hoá
  đơn.
- Tổng hoá đơn = tiền phòng + tiền dịch vụ + phụ thu − giảm giá.

**Cơ chế giảm giá — 3 nguồn cộng dồn, sau đó giới hạn không vượt quá tổng tiền**:
1. **Mã khuyến mãi**: lễ tân gõ tay mã (theo % hoặc số tiền cố định), chỉ những mã
   đang áp dụng và còn trong thời hạn mới chọn được từ danh sách gợi ý.
2. **Ưu đãi VIP tự động 10%**: hễ khách gắn nhãn VIP là tự cộng thêm, không cần nhập
   gì.
3. **Giảm giá thủ công**: lễ tân nhập số tiền → phải qua luồng yêu cầu — Quản lý
   duyệt (`invoice.discount.request` / `invoice.discount.approve`), người tạo yêu
   cầu không được tự duyệt của chính mình.

Ba nguồn trên **cộng dồn với nhau** rồi mới giới hạn về khoảng hợp lệ (không âm,
không vượt tổng tiền trước giảm giá) — không phải chọn 1 trong 3.

**Ghi nhận thanh toán**:
1. Mở hộp thoại Thu tiền từ hoá đơn, nhập số tiền (không vượt quá số còn thiếu),
   chọn phương thức: Tiền mặt hoặc Chuyển khoản (chuyển khoản bắt buộc nhập mã giao
   dịch, không được trùng mã đã dùng).
2. Xác nhận để ghi nhận.
3. Có thể thu **nhiều lần** cho tới khi đủ tổng tiền hoá đơn.

**Trạng thái hoá đơn suy ra từ tổng đã thu**: chưa thu đồng nào → Chưa thanh toán;
thu một phần → Thanh toán một phần; thu đủ hoặc vượt → Đã thanh toán.

**Huỷ hoá đơn**: chỉ gửi được yêu cầu huỷ khi hoá đơn **chưa có khoản thanh toán nào
đã hoàn tất** (nếu còn thanh toán, hệ thống chặn ngay từ giao diện, yêu cầu huỷ các
giao dịch thanh toán trước). Yêu cầu (`invoice.cancel.request`) → Quản lý duyệt
(`invoice.cancel.approve`) mới thật sự chuyển hoá đơn sang Đã huỷ.

**Huỷ giao dịch thanh toán**: chỉ áp dụng cho thanh toán đang **Hoàn tất**. Yêu cầu
(`payment.void.request`) → Quản lý duyệt (`payment.void.approve`) → giao dịch chuyển
sang Đã huỷ (không xoá bản ghi, giữ lịch sử); tổng đã thu và trạng thái hoá đơn được
tính lại ngay theo đúng ngưỡng ở trên.

**Quy tắc nghiệp vụ quan trọng khác**:
- **Chặn thu vượt số tiền còn lại** và **chặn bấm liên tục nhiều lần ra nhiều phiếu
  thu trùng** — đây là lỗi nghiêm trọng nhất từng gặp ở phiên bản trước, hệ thống
  hiện tại đã xử lý bằng giao dịch cơ sở dữ liệu có khoá và nút thu tiền tự khoá khi
  đang xử lý.
- Nếu đặt phòng có tiền cọc, khi lập hoá đơn lần đầu, tiền cọc **tự động trở thành
  một khoản thanh toán đã hoàn tất** trên hoá đơn (không cần thu lại thủ công).
- Nếu hoá đơn đã có thanh toán, những lần tính lại sau (do phát sinh dịch vụ/phụ thu
  mới) sẽ **giữ nguyên mức giảm giá cũ** thay vì tính lại theo mã khuyến mãi/VIP hiện
  tại — để tránh tổng hoá đơn tụt xuống dưới số tiền đã thu.

**Trạng thái liên quan**:

`InvoiceStatus`: Unpaid (Chưa thanh toán), PartiallyPaid (Thanh toán một phần), Paid
(Đã thanh toán), Cancelled (Đã huỷ). Chưa lập hoá đơn thì hiển thị "Chưa lập".

`PaymentMethod`: Cash (Tiền mặt), BankTransfer (Chuyển khoản).

**Lưu ý / trường hợp hay nhầm**:
- Xin duyệt giảm giá thủ công **nên thực hiện sau khi đã chốt mã khuyến mãi** cần
  dùng: nếu hoá đơn chưa có thanh toán nào, lúc Quản lý duyệt giảm giá tay, hệ thống
  tính lại từ đầu và có thể **không giữ được mã khuyến mãi đã nhập trước đó** nếu
  chưa lưu — nên khuyến nghị chốt khuyến mãi và giảm giá tay trong cùng một lần lập,
  hoặc thực hiện sau khi đã có ít nhất một khoản thanh toán (khi đó mức giảm được giữ
  cố định).

---

## 10. Khuyến mãi

**Vai trò nào dùng**: cần `promotion.manage` — theo cấu hình mặc định chỉ Quản lý.
Lễ tân không tạo/sửa được, chỉ **dùng** mã khi lập hoá đơn (mục 9).

**Mục đích**: quản lý các mã khuyến mãi áp dụng khi lập hoá đơn.

**Các bước thao tác**:
1. Thêm/sửa chương trình khuyến mãi: mã (tự động viết hoa), mô tả, loại giảm giá
   (Giảm theo phần trăm hoặc Giảm số tiền cố định), giá trị, ngày bắt đầu/kết thúc,
   cờ Đang áp dụng.
2. Không có nút xoá — muốn ngừng dùng thì sửa lại và bỏ tick "Đang áp dụng".

**Quy tắc nghiệp vụ quan trọng**:
- Mã khuyến mãi không được trùng nhau.
- Giá trị phải là số dương; nếu là loại phần trăm thì không được vượt quá 100.
- Ngày kết thúc phải từ ngày bắt đầu trở đi (cho phép bằng ngày bắt đầu).
- Khuyến mãi **không có điều kiện áp dụng theo loại phòng hay số đêm tối thiểu** —
  áp dụng chung cho mọi hoá đơn khi lễ tân nhập đúng mã.
- Khuyến mãi **chỉ áp dụng thủ công lúc lập hoá đơn**, không tự động gắn vào lúc đặt
  phòng.
- Mã hết hạn hoặc đã tắt vẫn còn trong danh sách quản lý (để xem lịch sử), nhưng **bị
  ẩn khỏi danh sách chọn nhanh** ở màn Hoá đơn.

**Trạng thái liên quan**: không lưu trạng thái cứng trong cơ sở dữ liệu, được tính
runtime mỗi lần tải trang theo thứ tự ưu tiên: **Đã tắt** (`IsActive = false`) →
**Chưa tới ngày** (hôm nay trước ngày bắt đầu) → **Hết hạn** (hôm nay sau ngày kết
thúc) → **Đang áp dụng** (còn lại). `PromotionType`: Percentage (Giảm theo phần
trăm), FixedAmount (Giảm số tiền cố định).

**Lưu ý / trường hợp hay nhầm**: ngày hiệu lực được so khớp theo **ngày lập hoá đơn**
(hoặc ngày trả phòng thực tế), không phải ngày check-in hay ngày đặt — một khuyến mãi
hết hạn giữa kỳ lưu trú dài ngày sẽ không dùng được nếu hoá đơn lập sau ngày hết hạn.

---

## 11. Báo cáo

**Vai trò nào dùng**: xem báo cáo cần `report.view`; xuất báo cáo cần `report.export`
— theo cấu hình mặc định chỉ Quản lý có cả hai.

**Mục đích**: thống kê doanh thu và công suất phòng theo khoảng ngày.

**Các bước thao tác**:
1. Chọn khoảng ngày Từ/Đến (có 3 nút bấm nhanh: 7 ngày / 30 ngày / Tháng này), hoặc
   tự chọn ngày trên lịch — đổi ngày là tự tải lại ngay.
2. Xem các thẻ chỉ số: Tiền phòng, Tiền dịch vụ, Phụ thu, Giảm giá, Tổng hoá đơn,
   Thực thu.
3. Xem khối "Công suất phòng hiện tại": tổng số phòng, đang trống, đã đặt, đang ở,
   tỷ lệ lấp đầy.
4. Xem bảng doanh thu theo từng ngày trong khoảng đã chọn.
5. Bấm "Xuất CSV" để lưu file báo cáo (định dạng CSV, mở được bằng Excel).

**Quy tắc nghiệp vụ quan trọng**:
- Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc, sai thì báo lỗi ngay, không cho
  chạy.
- Bảng doanh thu theo ngày **sắp xếp giảm dần theo tổng doanh thu** trong ngày (đây
  là yêu cầu bắt buộc của đề bài gốc, không phải sắp theo thứ tự ngày).
- Ngày trong khoảng mà không phát sinh doanh thu vẫn hiện đủ một dòng với giá trị 0,
  không bị nhảy cóc.
- Chỉ tính các hoá đơn chưa bị huỷ; hoá đơn Đã huỷ không được cộng vào doanh thu.
- Thực thu chỉ tính các khoản thanh toán đã Hoàn tất.

**Lưu ý / trường hợp hay nhầm**:
- **Khối "Công suất phòng hiện tại" là số chụp tại thời điểm xem báo cáo, không phụ
  thuộc vào khoảng ngày Từ/Đến đã chọn** — đổi khoảng ngày sẽ không làm số liệu công
  suất này thay đổi, chỉ có bảng doanh thu theo ngày mới đổi theo khoảng đã chọn.
- Nút "Xuất CSV" luôn hiển thị kể cả khi tài khoản chưa có quyền `report.export` —
  quyền chỉ được kiểm khi thực sự bấm xuất, lúc đó mới báo "Bạn không có quyền xuất
  báo cáo." nếu thiếu quyền.
- Chọn khoảng ngày không có dữ liệu vẫn hiển thị đầy đủ thông báo rõ ràng, không vỡ
  giao diện.

---

## 12. Người dùng

**Vai trò nào dùng**: cần `user.view` để xem, `user.manage` để thao tác — theo cấu
hình mặc định chỉ Admin.

**Mục đích**: quản lý tài khoản đăng nhập của nhân viên.

**Các bước thao tác**:
1. Danh sách hiện dạng thẻ/dòng, lọc theo từ khoá (tên/email) và theo chip vai trò
   (mỗi chip hiện luôn số lượng).
2. Thêm tài khoản mới: họ tên, email, mật khẩu, chọn vai trò.
3. Sửa thông tin, hoặc đặt lại mật khẩu (Admin gõ trực tiếp mật khẩu mới cho nhân
   viên, không cần biết mật khẩu cũ).
4. Khoá/Mở khoá tài khoản — có hộp thoại xác nhận trước khi khoá.

**Quy tắc nghiệp vụ quan trọng**:
- Email không được trùng (không phân biệt hoa/thường).
- Mật khẩu băm bằng BCrypt khi tạo mới và khi đặt lại.
- Không có khái niệm "xoá" tài khoản nhân viên — chỉ có Khoá (không đăng nhập được
  nữa) / Mở khoá, dữ liệu vẫn được giữ nguyên.
- Không cho tự khoá chính tài khoản đang đăng nhập.
- **Vai trò Quản trị viên không thể tạo/sửa/khoá/đặt lại mật khẩu qua giao diện dưới
  bất kỳ hình thức nào** — kể cả không thể gán vai trò Quản trị viên cho một tài
  khoản khác qua màn hình này. Tài khoản Admin chỉ được khởi tạo sẵn từ cấu hình
  triển khai hệ thống, không tạo thêm được từ trong ứng dụng.
- Gán vai trò đọc trực tiếp từ danh sách vai trò trong cơ sở dữ liệu (không hard-code
  trong code), nhưng luôn loại bỏ vai trò Quản trị viên khỏi danh sách chọn được.

**Trạng thái liên quan**: dùng cờ `IsActive` (Đang hoạt động / Đã khoá), không có
enum trạng thái riêng.

**Lưu ý / trường hợp hay nhầm**: quy tắc "không đụng được vào tài khoản Admin" mạnh
hơn thông thường — ngay cả Admin khác cũng không sửa/khoá được một tài khoản Admin
khác qua giao diện. Đây là chủ đích thiết kế để tránh một Admin tự nhân bản quyền cao
nhất hoặc tự khoá mình ra ngoài hệ thống.

---

## 13. Phân quyền

**Vai trò nào dùng**: cần `permission.manage` — theo cấu hình mặc định chỉ Admin.

**Mục đích**: cấu hình ma trận quyền cho từng vai trò, thay cho việc gán cứng quyền
theo tên vai trò trong code.

**Các bước thao tác**:
1. Chọn 1 vai trò từ danh sách 4 vai trò.
2. Tick/bỏ tick từng quyền trong danh sách đầy đủ (có thể lọc theo module hoặc tìm
   theo tên/mã quyền).
3. Nút "Chọn tất cả" có xử lý thông minh theo ngữ cảnh: nếu đang xem toàn bộ và vai
   trò đang cấu hình là vai trò quản trị hệ thống thì chỉ tick các quyền hệ thống
   (người dùng/phân quyền/nhật ký), tránh vô tình cấp luôn quyền nghiệp vụ; nếu là
   vai trò nghiệp vụ thì tick hết trừ các quyền quản trị và trừ 5 quyền "duyệt" (để
   không tự tạo xung đột yêu cầu-tự-duyệt chỉ bằng một cú bấm).
4. Bấm Lưu — có hộp thoại xác nhận, và sau khi lưu thành công hỏi thêm có muốn đăng
   xuất ngay để áp dụng quyền mới không.

**Quy tắc nghiệp vụ quan trọng**:
- **Đổi quyền không có hiệu lực ngay cho phiên đang mở** — chỉ áp dụng sau khi người
  dùng của vai trò đó đăng xuất và đăng nhập lại.
- Không cho lưu một vai trò hệ thống (cả 4 vai trò mặc định) với **0 quyền**.
- Luôn phải giữ ít nhất một vai trò còn quyền cấu hình phân quyền — nếu bộ quyền định
  lưu sẽ khiến không còn vai trò nào giữ quyền này, hệ thống từ chối lưu (tránh tự
  khoá đường vào lại chính màn Phân quyền).
- Chặn cấu hình xung đột tách nhiệm vụ: một vai trò không được vừa có quyền quản trị
  hệ thống (người dùng/phân quyền) vừa có quyền nghiệp vụ; các cặp quyền "yêu cầu" và
  "duyệt" tương ứng (huỷ đặt phòng, giảm giá, huỷ hoá đơn, huỷ thanh toán, bảo trì
  phòng) không được cùng gán cho một vai trò; người ghi nhận thanh toán không được
  đồng thời có quyền duyệt huỷ giao dịch; người lập hoá đơn không được đồng thời có
  quyền duyệt giảm giá.

**Trạng thái liên quan**: không có enum riêng, dùng cờ `IsAllowed` trên từng cặp
(vai trò, quyền).

**Lưu ý / trường hợp hay nhầm**: hệ thống hiện có 35 mã quyền, chia theo 11 nhóm chức
năng (Người dùng, Phân quyền, Nhật ký, Phòng, Đặt phòng, Lượt ở, Khách hàng, Dịch vụ,
Phụ thu, Khuyến mãi, Hoá đơn, Thanh toán, Báo cáo) — xem đầy đủ danh mục mã quyền tại
[`docs/PHAN_QUYEN_HE_THONG.md`](PHAN_QUYEN_HE_THONG.md) mục 5.

---

## 14. Nhật ký hệ thống

**Vai trò nào dùng**: cần `audit.view` — theo cấu hình mặc định chỉ Admin.

**Mục đích**: tra cứu lịch sử các thao tác nhạy cảm và thao tác quản trị trong hệ
thống.

**Các bước thao tác**:
1. Chọn khoảng ngày (mặc định 7 ngày gần nhất).
2. Lọc theo Hành động (danh sách sinh động từ đúng những hành động đã từng xảy ra,
   không phải danh sách cứng) và theo Người thực hiện.
3. Xem bảng kết quả: thời gian, người thực hiện + vai trò, hành động, đối tượng bị
   tác động, nội dung thay đổi, kết quả Thành công/Thất bại.

**Quy tắc nghiệp vụ quan trọng**:
- Giới hạn hiển thị 500 dòng gần nhất; vượt quá sẽ có gợi ý thu hẹp bộ lọc.
- Ghi nhật ký được thực hiện ở tầng Service (không thể "né" bằng cách gọi đường khác)
  cho các nhóm hành động: quản lý tài khoản nhân viên (tạo/sửa/khoá/mở khoá/đặt lại
  mật khẩu), quản lý hồ sơ khách hàng (tạo/sửa/xoá), quản lý phòng (tạo/sửa/đổi trạng
  thái/xoá), cấu hình phân quyền, và toàn bộ luồng yêu cầu — phê duyệt (gửi yêu cầu,
  duyệt, từ chối, thực thi).
- Nếu việc ghi nhật ký gặp lỗi, nghiệp vụ chính vẫn được thực hiện bình thường (lỗi
  ghi log không được phép làm hỏng nghiệp vụ) — nghĩa là về lý thuyết có thể có một
  số ít thao tác không để lại dấu vết nếu đúng lúc gặp sự cố khi ghi log.

**Trạng thái liên quan**: không có enum riêng (chỉ cờ Thành công/Thất bại của từng
dòng log).

**Lưu ý / trường hợp hay nhầm**: nếu người đang thực hiện thao tác sau đó bị xoá/khoá
khỏi hệ thống, dòng log cũ vẫn còn nhưng hiển thị người thực hiện là "Không rõ".

---

## 15. Phê duyệt

**Vai trò nào dùng**: menu chỉ hiện khi có ít nhất một trong các quyền duyệt
(`reservation.cancel.approve`, `invoice.discount.approve`, `invoice.cancel.approve`,
`payment.void.approve`) — theo cấu hình mặc định chỉ Quản lý. Quản lý theo cấu hình
mặc định cũng có `room.maintenance.approve` nên vẫn xử lý được yêu cầu bảo trì phòng
dù quyền này không nằm trong danh sách gate hiển thị menu.

**Mục đích**: xử lý tập trung mọi yêu cầu nghiệp vụ nhạy cảm cần người thứ hai xác
nhận, đảm bảo người yêu cầu không thể tự phê duyệt cho chính mình.

**Các bước thao tác**:
1. Lọc theo trạng thái (mặc định: Chờ duyệt), theo loại yêu cầu, theo từ khoá người
   gửi, theo khoảng ngày gửi.
2. Mỗi dòng hiện: loại yêu cầu, đối tượng liên quan, số tiền đề nghị giảm (nếu là
   giảm giá), người yêu cầu (phân biệt được là nhân viên hay khách hàng tự gửi), lý
   do.
3. **Duyệt**: xác nhận trong hộp thoại (không bắt buộc ghi chú thêm).
4. **Từ chối**: bắt buộc nhập lý do từ chối, không nhập thì không thực hiện được.

**5 loại yêu cầu và nghiệp vụ được thực thi khi Duyệt**:

| Loại yêu cầu | Quyền gửi yêu cầu | Quyền duyệt | Khi Duyệt, hệ thống thực hiện |
|---|---|---|---|
| Huỷ đặt phòng | `reservation.cancel.request` | `reservation.cancel.approve` | Chuyển đặt phòng sang Đã huỷ |
| Giảm giá hoá đơn | `invoice.discount.request` | `invoice.discount.approve` | Áp mức giảm giá vào hoá đơn |
| Huỷ hoá đơn | `invoice.cancel.request` | `invoice.cancel.approve` | Chuyển hoá đơn sang Đã huỷ (chỉ khi chưa có thanh toán) |
| Huỷ giao dịch thanh toán | `payment.void.request` | `payment.void.approve` | Chuyển giao dịch thanh toán sang Đã huỷ |
| Bảo trì phòng | `room.maintenance.request` | `room.maintenance.approve` | Chuyển phòng sang trạng thái Bảo trì |

**Quy tắc nghiệp vụ quan trọng**:
- **Người tạo yêu cầu không bao giờ tự duyệt được yêu cầu của chính mình** — hệ thống
  so sánh trực tiếp người tạo và người duyệt, không cho trùng.
- Một đối tượng chỉ có tối đa 1 yêu cầu đang Chờ duyệt tại một thời điểm cho cùng một
  loại — không gửi trùng được.
- Hai người cùng bấm Duyệt một yêu cầu cùng lúc: chỉ một người xử lý thành công,
  người còn lại nhận thông báo "yêu cầu này đã được xử lý" thay vì thực thi 2 lần.
- Nếu nghiệp vụ thực thi lúc Duyệt bị lỗi (ví dụ đối tượng không còn hợp lệ để huỷ
  nữa), yêu cầu **không** bị đánh dấu Đã duyệt — vẫn giữ Chờ duyệt để xử lý lại hoặc
  chuyển sang Từ chối.
- Yêu cầu huỷ đặt phòng do **khách hàng** tự gửi qua Cổng khách hàng và yêu cầu do
  **lễ tân** gửi tại quầy đi chung **một hàng đợi, một quy trình duyệt như nhau** —
  không có sự phân biệt đối xử giữa hai nguồn yêu cầu.
- Riêng loại **Bảo trì phòng**, luồng phê duyệt chỉ xử lý chiều đưa phòng **vào** bảo
  trì; chiều đưa phòng **ra khỏi** bảo trì không đi qua phê duyệt (xem mục 4).

**Trạng thái liên quan (`ApprovalRequestStatus`)**:

| Giá trị | Hiển thị |
|---|---|
| Pending | Chờ duyệt |
| Approved | Đã duyệt |
| Rejected | Đã từ chối |
| Cancelled | Đã huỷ |

`ApprovalRequestType`: ReservationCancel (Huỷ đặt phòng), InvoiceDiscount (Giảm giá
hoá đơn), InvoiceCancel (Huỷ hoá đơn), PaymentVoid (Huỷ giao dịch), RoomMaintenance
(Bảo trì phòng).

**Lưu ý / trường hợp hay nhầm**: nếu tự cấu hình một vai trò chỉ có riêng quyền
`room.maintenance.approve` (không kèm 4 quyền duyệt còn lại), vai trò đó **sẽ không
thấy menu "Phê duyệt"** trên sidebar dù về nguyên tắc vẫn đủ quyền xử lý yêu cầu bảo
trì — đây là khoảng lệch giữa điều kiện hiện menu và quyền xử lý thực tế, cần biết
trước khi tuỳ biến lại ma trận quyền.

---

## 16. Cổng khách hàng (Guest Portal)

**Vai trò nào dùng**: tài khoản khách (`GuestAccount`) — hoàn toàn tách biệt khỏi 4
vai trò nhân viên, không truy cập được bất kỳ màn hình quản trị nào.

**Mục đích**: cho phép khách tự phục vụ một số nghiệp vụ mà không cần liên hệ lễ tân.

Sau khi đăng nhập (xem mục 2), khách thấy 4 mục cố định trên thanh điều hướng riêng:
**Đặt phòng của tôi**, **Dịch vụ phòng**, **Hoá đơn của tôi**, **Hồ sơ của tôi**.

### Luồng 31 — Khách tự đặt phòng

**Các bước thao tác**: chọn ngày nhận/trả và số khách → hệ thống liệt kê **phòng cụ
thể còn trống** (sắp theo giá tăng dần) → chọn 1 phòng → xác nhận đặt.

**Quy tắc nghiệp vụ quan trọng**:
- Đơn tự đặt luôn khởi tạo ở trạng thái **Chờ xác nhận** — khách không có lựa chọn
  nào khác (khác với lễ tân tạo tại quầy có thể chọn thẳng Đã xác nhận).
- Khách không đặt cọc được lúc tự đặt (chỉ lễ tân mới thu cọc được).
- Khách thuộc danh sách **Cảnh báo (blacklist) bị chặn cứng**, không tự đặt được,
  phải liên hệ lễ tân.
- Một phòng đang có đơn **Chờ xác nhận** của người khác (chưa cần lễ tân xác nhận)
  cũng đã đủ để biến mất khỏi danh sách phòng trống của khách đến sau — đây là hành
  vi thật của hệ thống (tránh 2 người cùng giữ 1 phòng), không phải lỗi hiển thị.
- Đơn xác nhận từ Chờ xác nhận sang Đã xác nhận là việc của lễ tân, khách không tự
  xác nhận đơn của mình được.

### Luồng 32 — Khách tự yêu cầu huỷ đặt phòng

**Các bước thao tác**: từ "Đặt phòng của tôi", chọn đơn đang Chờ xác nhận hoặc Đã
xác nhận, bấm Yêu cầu huỷ, nhập lý do.

**Quy tắc nghiệp vụ quan trọng — điểm dễ hiểu nhầm nhất của cả hệ thống**:
- Khách **không được tự động huỷ ngay**. Yêu cầu của khách đi vào **đúng cùng một
  hàng đợi phê duyệt** mà lễ tân dùng khi xin huỷ đơn tại quầy (mục 5, mục 15) — đơn
  vẫn giữ nguyên trạng thái cho tới khi Quản lý duyệt.
- Không có giới hạn về mốc thời gian trước giờ nhận phòng khi gửi yêu cầu huỷ.
- Chỉ huỷ được đơn của chính mình (hệ thống tự xác thực quyền sở hữu, không tin dữ
  liệu gửi từ giao diện).
- Không gửi được yêu cầu huỷ trùng khi đơn đã có 1 yêu cầu đang Chờ duyệt.

### Luồng 33 — Khách tự gọi dịch vụ trong phòng

**Các bước thao tác**: vào "Dịch vụ phòng", chọn món theo nhóm, thêm vào giỏ, gửi.

**Quy tắc nghiệp vụ quan trọng**:
- Chỉ gọi được khi khách đang có lượt lưu trú ở trạng thái **Đang lưu trú** (đã nhận
  phòng thật, chưa trả phòng) — nếu chưa/không còn ở, toàn bộ menu và giỏ hàng bị
  khoá kèm thông báo rõ lý do.
- Đơn gửi từ Cổng khách hàng luôn khởi tạo ở trạng thái **Chờ xử lý**; khách chỉ tạo
  và xem, việc chuyển Đang làm/Hoàn tất là do nhân viên xử lý ở màn hình riêng (mục
  8) — khách không tự đổi trạng thái đơn của mình được.

### Luồng 34 — Khách xem hoá đơn của mình

**Các bước thao tác**: vào "Hoá đơn của tôi" để xem danh sách hoá đơn theo từng lượt
lưu trú đã từng nhận phòng (đơn chưa nhận phòng thì chưa có gì để hiển thị).

**Quy tắc nghiệp vụ quan trọng**: đây là màn hình **chỉ xem** — Cổng khách hàng hiện
chưa có tính năng thanh toán online, khách muốn thanh toán vẫn phải thực hiện qua lễ
tân tại quầy (mục 9).

### Luồng 35 — Khách xem/sửa hồ sơ cá nhân

**Các bước thao tác**: vào "Hồ sơ của tôi" để xem họ tên, số điện thoại, email, CCCD,
trạng thái VIP, ngày tạo tài khoản; đổi mật khẩu bằng cách nhập mật khẩu hiện tại +
mật khẩu mới + xác nhận.

**Quy tắc nghiệp vụ quan trọng**: khách **chỉ đổi được mật khẩu**, không tự sửa được
họ tên/số điện thoại/email/CCCD từ Cổng khách hàng — muốn cập nhật các thông tin này
phải nhờ lễ tân sửa hộ ở module Khách hàng (mục 7).

**Trạng thái liên quan**: dùng lại đúng các enum đã mô tả ở những mục tương ứng
(`ReservationStatus`, `ServiceOrderStatus`, `InvoiceStatus`) — Cổng khách hàng không
có enum trạng thái riêng.

**Lưu ý / trường hợp hay nhầm chung của Cổng khách hàng**:
- Không có xác minh OTP khi đăng ký (xem mục 2, luồng 3).
- Không có tính năng thanh toán online.
- Không sửa được thông tin cá nhân ngoài mật khẩu.
- Huỷ đặt phòng của khách và của lễ tân được đối xử **giống hệt nhau** (đều phải chờ
  Quản lý duyệt) — đừng mô tả nhầm là "khách tự huỷ ngay được, chỉ nhân viên mới cần
  duyệt".

---

## Bảng tổng hợp cuối tài liệu

| Nội dung | Số lượng |
|---|---|
| Tổng số luồng nghiệp vụ chính (đánh số ở mục 1) | 35 |
| Tổng số mục lớn (module) | 16 |
| Tổng số vai trò | 4 vai trò nhân viên (Admin, Manager, Receptionist, ServiceStaff) + 1 loại tài khoản khách (GuestAccount) = 5 |
| Tổng số nhóm enum trạng thái/phân loại xuất hiện trong hệ thống | 13 |
| Tổng số giá trị cụ thể trong 13 enum đó cộng lại | 50 |
| Tổng số mã quyền (PermissionCodes) | 35 |
| Tổng số loại yêu cầu trong luồng phê duyệt | 5 |

Danh sách 13 enum (tham chiếu nhanh, đã dùng đúng tên và nhãn hiển thị tiếng Việt
trong toàn bộ tài liệu ở trên): `ReservationStatus` (6 giá trị), `StayStatus` (3),
`RoomStatus` (5), `InvoiceStatus` (4), `PaymentStatus` (4), `PaymentMethod` (2),
`ServiceOrderStatus` (4), `ApprovalRequestStatus` (4), `ApprovalRequestType` (5),
`GuestTag` (3), `PromotionType` (2), `HousekeepingRequestStatus` (4),
`HousekeepingRequestType` (4).

Ghi chú riêng cho `HousekeepingRequestStatus`/`HousekeepingRequestType`: hai enum này
phục vụ nghiệp vụ "yêu cầu buồng phòng" (dọn phòng/thêm khăn/thêm nước) đã có đầy đủ
service xử lý ở tầng nghiệp vụ (kèm luật chuyển trạng thái Chờ xử lý → Đã tiếp nhận →
Hoàn tất/Đã huỷ và kiểm tra quyền), nhưng **tính đến thời điểm viết tài liệu này chưa
có màn hình (ViewModel/View) nào trong ứng dụng WPF gọi tới** — nên chưa đưa vào danh
sách 35 luồng có thể demo qua giao diện ở trên.
