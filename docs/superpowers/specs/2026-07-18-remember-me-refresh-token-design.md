# Remember me + Refresh token — Thiết kế

Ngày: 18/07/2026
Trạng thái: Đã duyệt, chờ triển khai

## Bối cảnh

JWT access token hiện hết hạn sau 120 phút (`Jwt:ExpireMinutes` trong `appsettings.json`). Khi hết hạn giữa phiên làm việc/test, `api/client.js` bắt lỗi 401, xoá phiên và đá thẳng về `/login` — không có cách nào khôi phục phiên mà không đăng nhập lại. Không có cơ chế "ghi nhớ đăng nhập".

## Mục tiêu

- Không bị đăng xuất giữa chừng khi access token hết hạn — tự động làm mới trong nền.
- Có tuỳ chọn "Ghi nhớ đăng nhập" để quyết định phiên sống bao lâu.
- Refresh token có thể thu hồi được (đúng thực hành chuẩn, không phải JWT tự ký không lưu trạng thái).

## Ngoài phạm vi

- Đăng nhập Google, tự đặt lại mật khẩu qua email — brainstorm riêng, chưa thiết kế ở đây.
- Phát hiện chuỗi token bị đánh cắp (chain revocation khi token cũ bị dùng lại) — chỉ từ chối token đã bị xoay vòng, không tự động thu hồi cả chuỗi. Đơn giản hoá phù hợp quy mô đồ án.

## Data model (backend)

Entity mới `RefreshToken` (theo `BaseEntity` sẵn có):

| Field | Kiểu | Ghi chú |
|---|---|---|
| Id | int | PK |
| Token | string | Chuỗi ngẫu nhiên (32 byte, mã hoá base64url), unique index |
| UserId | int | FK → User, `OnDelete: Cascade` (token vô nghĩa khi user bị xoá) |
| ExpiresAt | DateTime | 1 ngày (mặc định) hoặc 30 ngày (rememberMe=true) |
| CreatedAt | DateTime | |
| RevokedAt | DateTime? | null = còn sống |
| ReplacedByToken | string? | Token thay thế khi xoay vòng — null nếu chưa bị thay |

Migration mới: `AddRefreshTokens`.

## API

### `POST /api/auth/login` (sửa)

Request thêm field `rememberMe: bool` (mặc định `false`).
Response thêm field `refreshToken: string` bên cạnh các field hiện có.

### `POST /api/auth/refresh` (mới)

Không yêu cầu `[Authorize]` (access token có thể đã hết hạn — đó là lý do gọi endpoint này).

Request: `{ refreshToken: string }`

Xử lý:
1. Tìm `RefreshToken` theo `Token`. Không tồn tại → 401.
2. `RevokedAt != null` (đã bị thu hồi/dùng rồi) → 401 (từ chối, không tự phục hồi).
3. `ExpiresAt < now` → 401.
4. Hợp lệ: đánh dấu `RevokedAt = now`, `ReplacedByToken = <token mới>`. Tạo access token mới + `RefreshToken` mới (cùng `ExpiresAt` gốc theo `rememberMe` ban đầu — không tự "trẻ hoá" thời hạn mỗi lần refresh, tránh phiên sống vĩnh viễn nếu người dùng cứ hoạt động).

Response: giống `LoginResponse` (accessToken, refreshToken, expiresAt, thông tin user).

### `POST /api/auth/logout` (mới)

Request: `{ refreshToken: string }`

Đánh dấu `RevokedAt = now` cho token đó. Không lỗi nếu token không tồn tại/đã thu hồi (logout luôn "thành công" từ góc nhìn client).

## Cấu hình

`Jwt:ExpireMinutes`: giảm từ `120` xuống `30`. An toàn hơn theo thực hành chuẩn, không ảnh hưởng trải nghiệm vì refresh chạy nền im lặng.

## Frontend

### `utils/session.js`

- `saveSession(token, refreshToken, user)` — lưu thêm `refreshToken` vào `localStorage`.
- `getRefreshToken()` — đọc lại.
- `clearSession()` — xoá cả `refreshToken`.

### `pages/LoginPage.jsx`

Thêm checkbox "Ghi nhớ đăng nhập", gửi kèm `rememberMe` trong request login.

### `api/client.js` — interceptor tự làm mới

```
response interceptor:
  nếu lỗi 401 VÀ request gốc chưa từng retry VÀ url không phải /auth/login hoặc /auth/refresh:
    đánh dấu request đã retry (tránh vòng lặp vô hạn)
    nếu đang có 1 lượt refresh chạy: đợi promise đó xong rồi dùng token mới, retry request gốc
    nếu chưa có: gọi POST /auth/refresh với refreshToken hiện tại
      thành công: lưu token/refreshToken mới, retry request gốc với token mới
      thất bại: clearSession(), redirect /login (hành vi hiện tại, không đổi)
```

Khoá single-flight bằng 1 biến `Promise` module-scope dùng chung cho mọi request 401 đến cùng lúc — tránh gọi `/refresh` song song nhiều lần (mỗi lần xoay vòng sẽ vô hiệu hoá token của lần trước, gọi song song sẽ làm 1 trong 2 request thất bại giả).

### Đăng xuất (`MainLayout.jsx`)

Gọi `POST /api/auth/logout` với `refreshToken` hiện tại trước khi `clearSession()`. Best-effort — lỗi mạng không chặn việc xoá phiên local.

## Xử lý lỗi & trường hợp biên

- Refresh token hết hạn/bị thu hồi khi gọi `/refresh` → interceptor coi như thất bại, xử lý giống hiện tại (xoá phiên, về `/login`). Không hiện lỗi riêng — người dùng chỉ thấy màn login như đăng xuất bình thường.
- Nhiều tab cùng mở, tab A refresh trước tab B dùng token đã bị xoay vòng → tab B nhận 401 khi refresh lần đầu, coi như hết phiên, đá về login. Chấp nhận được cho quy mô đồ án (không cần đồng bộ đa tab).
- `rememberMe=false` nhưng người dùng đóng trình duyệt rồi mở lại trong vòng 1 ngày: vẫn còn đăng nhập vì refresh token còn hạn (do lưu ở `localStorage`, không phải `sessionStorage`) — đúng như kỳ vọng thông thường của "không tick nhớ" (chỉ hết khi thực sự không hoạt động quá 1 ngày, không phải hết khi đóng tab).

## Kế hoạch test

- Login không tick remember → refresh token 1 ngày; có tick → 30 ngày (kiểm tra `ExpiresAt` trong DB).
- Đợi access token hết hạn (hoặc chỉnh `ExpireMinutes` tạm xuống 1 phút để test nhanh) → gọi 1 API bất kỳ → xác nhận tự refresh, không bị đá về login.
- Gọi `/refresh` 2 lần liên tiếp với cùng 1 refresh token → lần 2 phải 401 (đã bị thu hồi khi xoay vòng ở lần 1).
- Đăng xuất → thử dùng lại refresh token cũ (gọi `/refresh` trực tiếp) → phải 401.
- Spam-click nhiều request cùng lúc khi access token vừa hết hạn → chỉ 1 lệnh gọi `/refresh` duy nhất được gửi (kiểm tra qua network log).
