# Role 4 Progress Report - 18/07/2026

## 1. Thông tin

- Người phụ trách: Trần Xuân Lộc (Role 4 - Frontend Room Map/Reservation/Check-in-out/Service/Invoice UI)
- Nhánh làm việc: `loc/fe`, đã merge vào `develop` (commit mới nhất `baa7c0c`)
- Tổng: 19 commit trong ngày, build sạch, đã test trực tiếp qua UI/Swagger trước khi merge

## 2. Migration cần chạy

Hai migration mới trong ngày, chạy từ thư mục `backend`:

```bash
dotnet ef database update --project HotelServiceManagement.Infrastructure --startup-project HotelServiceManagement.Api
```

- `20260716230235_AddRowVersionToReservation` — thêm cột `RowVersion` (concurrency token) vào `Reservations`.
- `20260718111358_AddRefreshTokens` — thêm bảng `RefreshTokens` (FK cascade tới `Users`, unique index trên `Token`).

Không đụng bảng/cột nào khác. Pull `develop` rồi chạy lệnh trên là đủ, không cần drop database.

## 3. Chống race-condition & sửa lỗi nghiệp vụ

| Commit | Nội dung |
|---|---|
| `f766cb2` | Thêm `RowVersion` cho `Reservation`, bắt `DbUpdateConcurrencyException` trả `409` rõ ràng thay vì để request sau âm thầm ghi đè request trước (VD: check-in và huỷ cùng lúc). |
| `7fa1d1f` | Siết quyền endpoint vận hành khớp thiết kế FE: bỏ Manager khỏi check-in/out, đặt phòng, hoá đơn, thanh toán (FE không có màn cho Manager dùng); thêm ServiceStaff vào `GET /api/stays/active` (cần để chọn đúng khách lúc tạo đơn dịch vụ). |
| `9113423` | `InvoiceResponse` trả thêm `DepositAmount`; `CreateInvoiceAsync` từ chối áp/đổi mã khuyến mãi khi hoá đơn đã `Paid` (409) — tránh làm `TotalAmount` thấp hơn số tiền đã thu thật. |
| `3d394b7` | Chuẩn hoá lỗi validate tự động của `[ApiController]` (`[Required]`, `[Range]`...) về cùng shape `{ message }` như lỗi nghiệp vụ khác, để `apiError()` phía FE đọc được message cụ thể thay vì rơi về "Máy chủ báo lỗi" chung chung. |
| `ec711b7` | Chống race condition trùng mã khuyến mãi khi 2 request tạo cùng mã gần như cùng lúc — bắt `DbUpdateException`, xác minh lại, trả `409` sạch thay vì lộ `500`. |

## 4. Sửa UI & chống spam-click

| Commit | Nội dung |
|---|---|
| `fa13450` | `BarChart` báo nhầm "chưa có dữ liệu" khi số liệu toàn 0 thật (VD 0 đồng cả kỳ, các tầng đều 0% lấp đầy) — nay vẽ biểu đồ phẳng đúng thực tế. |
| `e11b07b` | `ReportsPage` dùng chung `inputCls` từ `utils/ui` thay vì khai báo riêng (bản riêng thiếu `placeholder:text-ink-500/50`). |
| `c9256dc` | `GuestsPage`: đổi Tag khách về Bình thường thì tự xoá luôn ghi chú phân loại trong state, tránh lưu ngầm ghi chú cũ (VD lý do blacklist) khi bấm Lưu. |
| `f901438` | `InvoicePage` hiện dòng "Đã đặt cọc" khi hoá đơn có cọc — trước đó le tân không biết vì sao hoá đơn đã "Thanh toán một phần" ngay khi vừa tạo. |
| `a5ff65d` | Chống spam-click khi áp mã khuyến mãi — thêm `promoRef` đồng bộ (state cũ cập nhật bất đồng bộ nên bấm nhanh vẫn lọt 2 request). |
| `ea4557b` | Chống spam-click nút Huỷ/Không-đến ở `ReservationsPage` (thêm ref đồng bộ), dùng chung `inputCls`. |
| `adc0f8f` | 3 trang danh mục (Dịch vụ, Phụ thu, Khuyến mãi): thêm `ConfirmDialog` trước khi tắt một món đang dùng, thêm ref-guard chống spam-click cho nút Lưu và Bật/Tắt. |

## 5. Dashboard & báo cáo

| Commit | Nội dung |
|---|---|
| `8c49e9e` | Merge `develop` vào `loc/fe`, giải quyết 1 conflict thật ở `InvoiceService.cs`. |
| `110039f` | `DashboardPage`: bỏ dòng cảnh báo "số liệu minh hoạ - API chưa hỗ trợ" đã lỗi thời (backend `be/phat` đã trả đủ `arrivals/departures/revenue7d/alerts`); sửa lỗi chia 0 (`amount/maxRevenue = NaN`) khi cả 7 ngày doanh thu đều bằng 0. |
| `8aa49cf` | `BarChart` nhánh dọc: viết lại từ SVG (`preserveAspectRatio="none"` ép méo chữ) sang HTML/CSS flexbox — nhãn ngày (VD "18 thg 7") từng chồng chữ khi nhiều cột, nay không còn. |

## 6. Đăng nhập: remember-me + refresh token

| Commit | Nội dung |
|---|---|
| `c6f9991` | `LoginPage` thêm liên kết "Trang chủ" — trước đó là trang duy nhất trong app không có đường quay lại trang công khai. |
| `91c7ddf` | Spec thiết kế remember-me + refresh token (`docs/superpowers/specs/2026-07-18-remember-me-refresh-token-design.md`), đã duyệt trước khi code. |
| `e240d5e` | Backend: `RefreshToken` lưu DB (chuỗi random, không phải JWT), xoay vòng mỗi lần dùng (thu hồi token cũ, token mới giữ nguyên mốc hết hạn gốc). `POST /api/auth/refresh` làm mới access token, `POST /api/auth/logout` thu hồi token phía server. `LoginRequest.RememberMe` quyết định refresh token sống 1 ngày hay 30 ngày. `Jwt:ExpireMinutes` giảm từ 120 xuống 30. |
| `baa7c0c` | Frontend: `api/client.js` tự bắt `401`, gọi `/api/auth/refresh` rồi thử lại đúng request gốc — không văng người dùng ra `/login` giữa phiên. Nhiều request `401` gần như cùng lúc chỉ kích hoạt đúng 1 lần gọi `/refresh` (subscriber queue), không bị race condition. Checkbox "Ghi nhớ đăng nhập" ở `LoginPage`. Đăng xuất thu hồi refresh token phía server trước khi xoá phiên local. |

### Field FE cần giữ đúng cho phần auth

| Chỗ dùng | Field |
|---|---|
| Gửi lúc đăng nhập | `rememberMe` (boolean, optional, mặc định `false`) |
| Lưu sau đăng nhập/refresh | `refreshToken` (localStorage, tách khỏi `token`) |
| Gọi làm mới | `POST /api/auth/refresh` body `{ refreshToken }`, không cần `[Authorize]` |
| Gọi đăng xuất | `POST /api/auth/logout` body `{ refreshToken }`, luôn trả `200` |

Trang nào tự gọi `/api/auth/login` riêng (không qua `client.js` chung) cần lưu thêm `refreshToken` mới nhận được auto-refresh.

## 7. Testing đã thực hiện

- Race condition: Huỷ/Không-đến/Nhận-phòng cùng lúc → `409` rõ ràng, không ghi đè âm thầm; tạo mã khuyến mãi trùng đồng thời → `409` thay vì `500`.
- Phân quyền: xác nhận Manager không còn thấy/gọi được các trang Check-in/out, đặt phòng, hoá đơn, thanh toán; ServiceStaff gọi được `GET /api/stays/active`.
- Spam-click: bấm liên tục nút Huỷ/Không-đến/Áp mã KM/Lưu-Bật-Tắt danh mục — chỉ 1 request thật sự được gửi.
- Dashboard: tạo đặt phòng + thanh toán thật, kiểm tra KPI và biểu đồ doanh thu 7 ngày hiển thị đúng, không còn NaN.
- BarChart: 7 nhãn ngày không chồng chữ; trường hợp toàn 0 vẽ đúng biểu đồ phẳng thay vì báo nhầm "chưa có dữ liệu".
- Auth: đăng nhập tick/không tick "Ghi nhớ" → refresh token đúng 30 ngày/1 ngày; access token hết hạn giữa phiên tự làm mới; refresh token bị thu hồi/dùng lại → `401` và tự chuyển `/login`; đăng xuất thu hồi token ngay; 5 request đồng thời lúc access token hết hạn chỉ gọi `/refresh` đúng 1 lần.

## 8. Việc cần làm cho team

1. `git pull` nhánh `develop`, chạy 2 migration ở mục 2.
2. Không cần đổi gì ở FE nếu đang dùng chung `client.js`/`session.js` — auto-refresh đã chạy ngầm.
3. Nếu có trang nào tự gọi axios riêng cho `/api/auth/login`, báo lại để nối thêm logic refresh — tránh bị out phiên khi JWT hết hạn sau 30 phút (trước đây 120 phút).
