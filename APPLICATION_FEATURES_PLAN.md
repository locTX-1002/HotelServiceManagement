# Kế hoạch Tính năng (Application Features Plan) - HSMS

Trạng thái các module theo TeamAssignment (docs/TeamAssignment.md) và sprint 05/07 → 12/07. Cập nhật cột Trạng thái khi có thay đổi lớn; tiến độ chi tiết theo ngày nằm trên Notion board.

| Module | API | UI | Owner API / UI | Hạn | Trạng thái |
|---|---|---|---|---|---|
| Setup (repo, CI, DB, scaffold, Docker) | - | - | Cả nhóm (Day 0) | 05/07 | ✅ Xong |
| Auth: login JWT + 4 role + /auth/me | Role 1 | Role 3 | Phát / Phúc | T2 06/07 | Chưa |
| Room Type + Room CRUD + status | Role 2 | Role 3 | Khoa / Phúc | T2-T3 | Chưa |
| Room Map (GET /api/rooms/map) | Role 2 | Role 4 | Khoa / Lộc | T2 | UI xong trước (mock), chờ API |
| Guest CRUD | Role 1 hỗ trợ | Role 5 | Phát / Tú | T3 | Chưa |
| Reservation + available-rooms + BR03 | Role 2 | Role 4 | Khoa / Lộc | T3 | Rule BR03 + 11 unit test đã sẵn trong BookingRules |
| Check-in / Check-out (BR04, BR05, BR09) | Role 1 | Role 4 | Phát / Lộc | T3-T4 | Chưa |
| Service category/item/order (BR06) | Role 2 | Role 4 + Role 5 | Khoa / Lộc + Tú | T4 | Chưa |
| Invoice + Payment (BR07, BR08) | Role 1 | Role 4 | Phát / Lộc | T4-T5 | Chưa |
| Reports: dashboard/occupancy/revenue (BR10) | Role 2 | Role 3 | Khoa / Phúc | T5 | Chưa |
| Seed demo + test + docs + demo script | - | - | Tú (Role 5) | T5-T7 | Đang chạy dần |

## Mốc cứng

- **T5 09/07 20:00** — chạy demo 15 bước lần 1
- **T6 10/07 12:00** — freeze tính năng
- **CN 12/07** — nộp

## Thứ tự cắt nếu trễ (giữ trên, bỏ dưới)

Login → Room → Room Map → Reservation → Check-in → Service → Check-out + Invoice → Payment. Report chỉ cần dashboard cơ bản. Bỏ được: export, animation, advanced filter, audit log, deploy online.
