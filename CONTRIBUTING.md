# Quy ước làm việc — Group 2 SE1919

## Nhánh

| Nhánh | Mục đích |
|---|---|
| `main` | Bản ổn định để demo. Chỉ merge từ `develop`. |
| `develop` | Nhánh tích hợp. Mọi feature merge vào đây qua PR. |
| `feature/<tên>` | Mỗi task 1 nhánh, ví dụ `feature/auth-jwt`, `feature/room-map` |
| `fix/<tên>` | Sửa bug, ví dụ `fix/overlap-booking-check` |

## Commit message

Conventional Commits, tiếng Việt hoặc tiếng Anh nhất quán trong 1 PR:

```
feat(room): thêm API cập nhật trạng thái phòng
fix(reservation): chặn đặt phòng trùng ngày (BR03)
chore: cập nhật README
```

## Pull Request checklist (Report 2 §7.3)

- [ ] Build xanh trên máy local và CI
- [ ] Đã tự test API bằng Swagger (happy path + ít nhất 1 ca lỗi)
- [ ] Trang frontend gọi đúng API (nếu là PR frontend)
- [ ] KHÔNG commit secrets, JWT key, connection string production
- [ ] Có migration đi kèm nếu đổi schema (CHỈ Lộc tạo migration)
- [ ] Tuân thủ business rules BR01–BR10
- [ ] Ít nhất 1 người khác review trước khi merge

## Quy ước code

- **C#**: PascalCase cho class/method, camelCase biến local, `_camelCase` field private. 1 class / 1 file.
- **React**: functional component + hooks, file component PascalCase.jsx.
- **REST**: số nhiều, kebab-case (`/api/room-types`), status code đúng nghĩa (400/401/403/404/409).
- Chỉ **Lộc** (Database Designer) được tạo EF Core migration — ai đổi entity phải PR trước cho Lộc.

## WIP limit

1 task / người tại 1 thời điểm. Review PR của người khác trong vòng 6 tiếng.
