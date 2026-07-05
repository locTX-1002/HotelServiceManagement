# HSMS Frontend

React (Vite) + Tailwind CSS + Axios + React Router.

```bash
npm install
copy .env.example .env    # macOS/Linux: cp .env.example .env
npm run dev               # http://localhost:5173
```

Backend phải chạy trước ở http://localhost:5000 — xem [README gốc](../README.md).

- `src/api/client.js` — axios instance, tự gắn JWT, tự redirect khi 401
- `src/utils/roomStatus.js` — màu 5 trạng thái phòng + `formatVnd`
- `src/layouts/MainLayout.jsx` — sidebar + topbar
- Route và owner từng màn hình: xem [docs/routes-colors.md](../docs/routes-colors.md)
