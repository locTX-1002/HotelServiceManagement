// Quyền XEM theo vai trò - nguồn sự thật duy nhất cho menu (MainLayout),
// chặn route (RequireRole) và trang đích sau login (LoginPage).
// Đây chỉ là lớp UX: chốt chặn thật vẫn là [Authorize(Roles)] phía backend.
const ALL = ['Admin', 'Manager', 'Receptionist', 'ServiceStaff']

// Theo mô tả role trong seed backend + demo flow của TeamAssignment:
// Manager chỉ xem tổng quan/báo cáo, Receptionist vận hành đặt phòng - check-in,
// ServiceStaff chỉ lo dịch vụ, Admin thấy tất cả.
// /dashboard đọc chung API /api/reports/dashboard - backend chỉ cho Admin,Manager
// ([Authorize(Roles = "Admin,Manager")] ở ReportsController) nên FE phải khớp, không thì Receptionist vào là dính 403.
export const ROUTE_ROLES = {
  '/dashboard': ['Admin', 'Manager'],
  '/rooms/map': ALL,
  '/rooms': ['Admin', 'Manager'],
  '/rooms/types': ['Admin', 'Manager'],
  '/reservations': ['Admin', 'Receptionist'],
  '/reservations/new': ['Admin', 'Receptionist'],
  '/checkin-checkout': ['Admin', 'Receptionist'],
  // Khớp [Authorize] backend: PUT /api/guests cho Admin,Manager,Receptionist
  '/guests': ['Admin', 'Manager', 'Receptionist'],
  '/service-orders': ['Admin', 'Receptionist', 'ServiceStaff'],
  // Khớp [Authorize] backend: POST/PUT /api/service-items cho Admin,Manager,ServiceStaff
  '/service-items': ['Admin', 'Manager', 'ServiceStaff'],
  '/invoices': ['Admin', 'Receptionist'],
  '/reports': ['Admin', 'Manager'],
}

export const ROLE_LABEL = { Admin: 'Quản trị', Manager: 'Quản lý', Receptionist: 'Lễ tân', ServiceStaff: 'NV dịch vụ' }

export const canAccess = (role, path) => {
  // Phiên cũ chưa lưu role -> không chặn nhầm người, backend sẽ chặn thật khi gọi API
  if (!role) return true
  const roles = ROUTE_ROLES[path]
  return !roles || roles.includes(role)
}

// Trang đích mặc định sau khi đăng nhập: ai không được xem Tổng quan thì về Sơ đồ phòng
export const homeFor = (role) => (canAccess(role, '/dashboard') ? '/dashboard' : '/rooms/map')
