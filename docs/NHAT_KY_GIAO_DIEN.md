# Nhật ký nhánh giao diện WPF (`loc/wpf-app`)

> Người làm: Trần Xuân Lộc. Ghép vào `develop` ngày 23/07/2026, gồm 31 commit.
> File này ghi lại đã làm gì và **vì sao**, để người sau đọc code không phải đoán.

## 1. Tóm tắt

Nhánh này dựng toàn bộ tầng giao diện của app: từ khung shell, theme, tới 6 màn
nghiệp vụ. Cuối nhánh có một lần hợp nhất lớn với tầng service Phát dựng song
song trên `develop`.

| Nhóm việc | Số commit |
|---|---|
| Màn hình mới | 8 |
| Sửa lỗi | 9 |
| Chỉnh giao diện theo góp ý | 6 |
| Nghiệp vụ bổ sung | 2 |
| Tài liệu | 3 |
| Hợp nhất với develop | 3 |

## 2. Màn hình đã làm

### Trang chủ

Landing kiểu trang giới thiệu khách sạn, không phải dashboard số liệu.

- Hero ba ảnh xoay vòng, lời chào đổi theo buổi trong ngày
- **Thanh tra cứu phòng trống** nổi lên đáy hero — chọn ngày và số khách rồi
  tìm, bấm Đặt ngay là mở dialog đặt phòng với ngày đã điền sẵn
- Khối giới thiệu, số phòng và số hạng đếm thẳng từ database
- Lưới hạng phòng dạng mosaic: một ô lớn, hai ô dọc, một ô ngang
- Dải số liệu vận hành cuối trang, ô việc quá hạn tô đỏ và bấm được để nhảy sang
  màn check-in/out

Tên khách sạn và năm thành lập đọc từ mục `Hotel` trong `appsettings.json`
(`Services/HotelInfo.cs`), đổi không phải build lại.

### Phòng và Loại phòng

- **Sơ đồ phòng**: thẻ ảnh phủ kín ô, số phòng và trạng thái đè lên đáy ảnh
- **Danh sách phòng**: master-detail có gallery ảnh, chip lọc trạng thái kèm số đếm
- **Loại phòng**: lưới thẻ hai cột, ảnh lớn, giá nổi bật, nút Sửa/Xoá ngay trên thẻ

### Khách hàng

Danh sách, tìm kiếm, thêm/sửa, tag VIP và cảnh báo.

### Đặt phòng — hai tab

**Tab Danh sách**: mỗi đơn một thẻ có ảnh, chip lọc trạng thái kèm số đếm, bốn
nút hành động chỉ bung ra ở thẻ đang chọn. Nút không hợp lệ với trạng thái hiện
tại thì ẩn hẳn, nên không bấm nhầm được.

**Tab Lịch phòng**: mỗi dòng một phòng, mỗi cột một ngày trong tuần, thanh màu
trải đúng số đêm. Bấm ô trống là mở dialog đặt đúng phòng đúng ngày, danh sách
phòng trống được tra sẵn. Ngày đã qua không cho đặt. Chỉ vẽ đơn đang giữ phòng.

### Check-in / Check-out

Một dòng thời gian duy nhất thay vì hai cột, xếp theo độ ưu tiên: quá hạn lên
đầu, rồi việc hôm nay. Chip lọc Tất cả / Hôm nay / Sắp đến / Đang ở / Quá hạn.
Dòng quá hạn có vạch đỏ bên trái.

Bảng làm việc bên phải hiện thông tin khách và **tiền tạm tính trước khi bấm** —
trước đây lễ tân bấm check-out mà không biết thu bao nhiêu.

## 3. Nghiệp vụ bổ sung

### Gia hạn lưu trú

Khách đang ở muốn ở thêm thì trước đây **không có đường nào**: sửa đơn bị chặn
hai lớp vì đã check-in và đã có lượt lưu trú, nên lễ tân phải check-out rồi tạo
đơn mới — hỏng lịch sử và sai doanh thu.

Giờ có nút Gia hạn đổi thẳng ngày trả, kiểm tra phòng có bị khách khác đặt chồng
trong khoảng ở thêm không, báo giá luôn tổng tiền theo ngày mới. Rút ngắn ngày
trả cũng làm được, kèm ghi chú không tự hoàn tiền.

### Phụ thu / kiểm đồ

Ghi lại thứ khách làm hỏng hoặc mất trước khi cho trả phòng. Bảng giá 8 mục đã
có sẵn trong database. **Đơn giá chụp lại lúc ghi** nên sau này đổi bảng giá
không ảnh hưởng dòng cũ. Ghi nhầm xoá được nhưng chỉ khi khách chưa trả phòng.

### Đếm đêm khi khách ở quá hạn

Trước đây số đêm dừng ở ngày trả ghi trên đơn, khách ở quá 3 đêm vẫn tính như
đúng hạn. Giờ tính tới hôm nay, và có dòng đỏ ghi rõ mấy đêm quá hạn để lễ tân
giải thích được với khách.

## 4. Lỗi đã sửa, kèm nguyên nhân

| Lỗi | Nguyên nhân |
|---|---|
| **Form khách mới không hiện** khi tra CCCD không ra kết quả | `ShowNewGuestFields` phụ thuộc `MatchedGuest`, mà setter chỉ bắn `PropertyChanged` khi giá trị đổi. Lần tra đầu `null → null` nên setter im lặng. Hệ quả: **chưa từng đặt phòng được cho khách mới** |
| Ba ô nhập khách mới không có nhãn | Dùng `hc:InfoElement.Placeholder`, nhưng style TextBox chung của app thay hẳn template gốc của HandyControl nên placeholder không bao giờ được vẽ |
| Hàng DataGrid đang chọn đọc không ra chữ | HandyControl gán `RowStyle` **tường minh**, mà gán tường minh thắng style ngầm. Phải định nghĩa style `DataGrid` rồi tự gán `RowStyle` bên trong |
| Nút bo tròn vẽ thành elip méo | WPF không kẹp `CornerRadius` như CSS `border-radius`. Sau đó chốt bo 4px toàn app nên bỏ hẳn |
| Panel chi tiết không hiện khi chọn dòng | Style đặt mặc định `Collapsed` và trigger duy nhất cũng đặt `Collapsed`, không có đường nào ra `Visible` |
| Ảnh loại phòng lưu theo **tên** | Hạng mới tên lạ rơi vào nhóm "standard", gán ảnh cho nó là **đè mất ảnh của Standard**. Đổi sang lưu theo id |
| Chip lọc check-in/out không sáng khi chọn | Thiếu `IsSelected`, hai màn kia đã có |
| Lỗi thiếu cấu hình báo thành lỗi SQL Server | Bắt chung mọi exception rồi đổ tại SQL. Người đọc sẽ đi sửa chuỗi kết nối trong khi lỗi là thiếu `appsettings.Local.json` |

## 5. Hợp nhất với tầng service của Phát

Hai nhánh chạy song song mà không ai biết bên kia làm gì: Phát dựng 14 service
backend trên `develop`, nhánh này dựng giao diện. Bốn chuỗi Guest, Reservation,
Stay, Surcharge bị trùng vì cả hai cùng cần.

**Đã chốt giữ bản của Phát**, giao diện sửa theo chữ ký đó. Tám chuỗi mới của
Phát vào nguyên vẹn vì chúng chỉ dùng repository riêng, không đụng bốn chuỗi trên.

Bổ sung vào tầng của Phát bảy khả năng mà giao diện cần nhưng chưa có — **viết
thêm, không sửa phần đã chạy**:

| Bổ sung | Dùng ở đâu |
|---|---|
| `ReservationDAO.GetAvailableRoomsAsync` | Đặt phòng, Lịch phòng, Trang chủ |
| `ReservationService.NoShowAsync` | Đặt phòng |
| `ReservationService.StatusText` | Danh sách và lịch phòng |
| `StayDAO.GetArrivalsAsync` | Check-in/out, Trang chủ |
| `StayDAO.ExtendAsync` | Gia hạn |
| `GuestDAO.FindExactAsync` | Dialog đặt phòng |
| `SurchargeDAO.GetTotalsAsync` | Check-in/out |

Chi tiết va chạm ghi ở [HOP_NHAT_NHANH.md](HOP_NHAT_NHANH.md).

## 6. Hai chỗ giao diện đang thiếu sau khi hợp nhất

1. **Sửa đơn không đổi khách được nữa.** `IReservationService.UpdateAsync` của
   Phát không nhận `guestId`. Muốn có lại thì nhờ Phát thêm tham số.
2. **Tiền cọc chưa nhập được lúc tạo đơn.** `CreateAsync` có sẵn tham số
   `depositAmount` và `depositPaymentMethod`, giao diện đang truyền `null` vì
   dialog chưa có ô nhập. Thêm hai ô là chạy ngay — tính năng backend đã có sẵn.

## 7. Quy ước để lại cho nhóm

- **Bo góc 4px cho mọi hình chữ nhật.** Chỉ hình tròn hoàn hảo mới dùng bán kính
  lớn: avatar, logo, chấm trạng thái, mũi tên gallery
- **Thẻ ảnh dùng chung** `TileShade` / `TileName` / `TileMeta` trong
  `Themes/Controls.xaml` — trang chủ và sơ đồ phòng cùng trỏ về đây
- **Chip lọc** có `IsSelected` và số đếm, dùng `ItemsControl` thay vì gõ tay
  từng nút
- **`QuickChip`** cho nút chọn nhanh, **`SegmentLeft`/`SegmentRight`** cho cặp
  lựa chọn thay dropdown khi chỉ có 2–3 phương án
- Định dạng danh sách mặc định là **card-row có ảnh**, không phải DataGrid trơn

## 8. Kiểm tra trước khi ghép

- `dotnet build` 0 lỗi, 0 cảnh báo
- 19/19 test của Phát pass
- App chạy thật, đăng nhập được, đi hết các màn

Tài khoản admin lấy từ `appsettings.Local.json` của từng máy — file này đã
gitignore, mỗi người tự tạo. Ba tài khoản demo (manager, receptionist, service)
**bị khoá lại mỗi lần khởi động** theo thiết kế bootstrap của Phát, nên muốn thử
phân quyền phải tạo tài khoản thật ở màn Người dùng khi màn đó xong.
