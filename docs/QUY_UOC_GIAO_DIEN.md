# Quy ước giao diện — chống "format kiểu AI"

Áp cho MỌI màn hình WPF trong app. Mục tiêu: nhìn như sản phẩm được thiết kế có chủ đích,
không phải template AI sinh hàng loạt. Đọc trước khi làm module, soi lại trước khi mở PR.

## 1. Hệ bo góc — MỘT hệ duy nhất, có lý do

| Thành phần | Bo góc |
|---|---|
| Nút (Primary/Ghost) | pill (999) |
| Ô nhập (TextBox/PasswordBox/ComboBox) | 8 |
| Card, dialog, panel nội dung | 12 |
| Item điều hướng, khối phụ | 10 |
| Badge | pill (999) |

Cấm chế thêm giá trị mới. Trộn "nút vuông cạnh card tròn" = fail review.

## 2. Chiều sâu — double-bezel, không đổ bóng gắt

- Card/dialog KHÔNG dính phẳng vào nền: nền cửa sổ là `SurfaceBrush`, khối nội dung là
  `Card` + `SoftShadow` (bóng khuếch tán rộng, mờ ~10%) — như tấm kính đặt trên khay.
- CẤM bóng đen gắt (ShadowDepth lớn, Opacity > 0.15), cấm viền xám 1px generic —
  viền duy nhất là `LineBrush` hairline.

## 3. Chữ — cấp bậc rõ, không hét

- Chỉ dùng 4 cấp trong `Typography.xaml` (PageTitle/SectionTitle/Body/Caption + FieldLabel).
- Tiêu đề to nhưng KHÔNG in đậm cả câu dài; nhấn bằng weight + màu, không bằng cỡ chữ khổng lồ.
- Label field: chữ hoa nhỏ 10.5 — CHỈ dùng trên ô nhập, không rải khắp nơi làm "eyebrow".

## 4. Màu — một accent, khoá cứng

- Accent duy nhất: xanh rêu `BrandBrush`. Không thêm màu nhấn thứ hai.
- Success/Warning/Danger/Info CHỈ xuất hiện dưới dạng badge/banner trạng thái — không làm màu trang trí.
- Cấm hardcode hex trong module (đã có rule, nhắc lại vì đây là lỗi AI phổ biến nhất).

## 5. Trang trí — có chủ đích hoặc không có gì

- CẤM: hình tròn/blob mờ trôi nổi, gradient nhiều màu, hoạ tiết ngẫu nhiên "cho đẹp",
  icon to đùng giữa trang, dòng chữ mờ nghiêng làm nền.
- Được: khối typographic có chủ đích (monogram FU), hairline phân vùng đúng chỗ có nội dung.
- Mỗi vùng trống phải là khoảng thở có nhịp (padding 24/28), không phải chỗ "quên chưa lấp".

## 6. Bảng & danh sách — thở được

- DataGrid: RowHeight ≥ 44, không kẻ ô (`GridLinesVisibility="None"`), header chữ Caption.
- Không nhồi 10 cột — tối đa ~7, phần chi tiết để dialog/panel.
- Danh sách rỗng phải có empty-state (1 câu hướng dẫn), đang tải phải có LoadingCircle.

## 7. Chuyển động — vi mô, vật lý, không màu mè

- Nút bấm có phản hồi vật lý: scale 0.98 khi nhấn (đã có sẵn trong style — đừng gỡ).
- Không animation xoay/bay/nhấp nháy. Growl toast là chuyển động duy nhất cỡ lớn.

## 8. Checklist trước khi mở PR màn hình mới

- [ ] Không hardcode màu/cỡ chữ/bo góc ngoài token
- [ ] Có đủ 3 trạng thái: loading / rỗng / lỗi
- [ ] Dialog dùng double-bezel (Card + SoftShadow trên nền Surface)
- [ ] Không trang trí vô chủ đích, không icon khổng lồ
- [ ] Nút chính mỗi màn chỉ MỘT (PrimaryButton), còn lại Ghost
- [ ] Chạy thử cửa sổ ở MinSize 1100×680 — không vỡ layout
