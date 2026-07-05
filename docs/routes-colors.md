# Hợp đồng chung: Routes + Màu trạng thái phòng

> File này là nguồn sự thật duy nhất. Đổi gì ở đây phải báo cả nhóm trong standup.

## 8 routes frontend

| Route | Màn hình | Owner UI | Ngày |
|---|---|---|---|
| `/login` | Login | Khoa | T2 06/07 |
| `/dashboard` | Dashboard cards | Phúc | T2 06/07 |
| `/rooms/map` | Visual Room Map | Tú | T2 06/07 |
| `/rooms` | Room / Room Type management | Khoa | T3 07/07 |
| `/reservations` | Reservation flow + danh sách | Tú (+Phúc danh sách) | T3–T4 |
| `/checkin-checkout` | Check-in / Check-out | Khoa | T4 08/07 |
| `/service-orders` | Service Orders | Tú | T4 08/07 |
| `/reports` | Reports (bảng + card) | Khoa | T5 09/07 |

## Màu 5 trạng thái phòng (constant: `src/utils/roomStatus.js`)

| Status | Màu | Tailwind |
|---|---|---|
| Available | Xanh lá | `green-500` |
| Reserved | Xanh dương | `blue-500` |
| Occupied | Đỏ | `red-500` |
| Cleaning | Cam | `orange-500` |
| Maintenance | Xám | `gray-500` |

## Ports

- Backend: **http://localhost:5000** (Swagger: `/swagger`, health: `/health`)
- Frontend: **http://localhost:5173** (đã cấu hình CORS sẵn ở backend)
