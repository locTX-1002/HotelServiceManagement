using System;
using System.Collections.Generic;
using BusinessObjects.Entities;

namespace FUHotelManagementWPF.ViewModels.AuditLogs
{
    /// <summary>
    /// Mot dong nhat ky da doi sang chu tieng Viet. Ma hanh dong (<c>user.lock</c>...) la
    /// dinh danh ky thuat cho code doi chieu, khong phai thu cho nguoi doc.
    /// </summary>
    public sealed class AuditLogRow
    {
        private static readonly Dictionary<string, string> ActionNames = new()
        {
            ["user.create"] = "Tạo tài khoản",
            ["user.update"] = "Sửa tài khoản",
            ["user.lock"] = "Khoá tài khoản",
            ["user.unlock"] = "Mở khoá tài khoản",
            ["user.reset_password"] = "Đặt lại mật khẩu",
            ["guest.create"] = "Tạo hồ sơ khách",
            ["guest.update"] = "Sửa hồ sơ khách",
            ["guest.delete"] = "Xoá hồ sơ khách",
            ["guest.activate_account"] = "Cấp tài khoản khách",
            ["room.create"] = "Tạo phòng mới",
            ["room.update"] = "Sửa thông tin phòng",
            ["room.delete"] = "Xoá/Ngừng dùng phòng",
            ["room.change_status"] = "Đổi trạng thái phòng",
            ["permission.update"] = "Đổi quyền của vai trò",
            ["approval.request"] = "Gửi yêu cầu duyệt",
            ["approval.approve"] = "Duyệt yêu cầu",
            ["approval.reject"] = "Từ chối yêu cầu",
            ["approval.execute"] = "Thực hiện sau khi duyệt",
        };

        public AuditLogRow(AuditLog log) => Log = log;

        public AuditLog Log { get; }

        public DateTime CreatedAt => Log.CreatedAt;

        /// <summary>Chua co ten thi hien thang ma - van hon la de trong khi them hanh dong moi.</summary>
        public string ActionText => ActionNames.TryGetValue(Log.ActionCode, out var name)
            ? name
            : Log.ActionCode;

        /// <summary>Tai khoan da bi xoa thi UserId ve null (khoa ngoai SetNull), khong mat dong nhat ky.</summary>
        public string ActorText => Log.User == null
            ? "Không rõ"
            : $"{Log.User.FullName} ({RoleText(Log.User.Role?.RoleName)})";

        public string TargetText => Log.EntityId == null ? Log.EntityType : $"{Log.EntityType} #{Log.EntityId}";

        /// <summary>Gop truoc/sau vao mot cot: doc mot dong la thay duoc doi cai gi.</summary>
        public string ChangeText => (Log.OldValues, Log.NewValues) switch
        {
            (null or "", null or "") => "—",
            (null or "", var moi) => moi!,
            (var cu, null or "") => cu!,
            var (cu, moi) => $"{cu}  →  {moi}",
        };

        public string SummaryText
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ChangeText) || ChangeText == "—")
                    return TargetText;
                return ChangeText;
            }
        }

        /// <summary>Chi tiet thao tac da lam sach trung lap va ma ky thuat cho nguoi dung de doc.</summary>
        public string DetailText
        {
            get
            {
                var text = SummaryText;
                if (!string.IsNullOrWhiteSpace(Log.ActionCode) && text.StartsWith(Log.ActionCode, StringComparison.OrdinalIgnoreCase))
                {
                    var idx = text.IndexOfAny(new[] { '-', '·', ':' });
                    if (idx >= 0 && idx < text.Length - 1)
                        text = text[(idx + 1)..].Trim();
                }
                return text;
            }
        }

        public string ResultText => Log.Succeeded ? "Thành công" : "Thất bại";
        public bool IsSuccess => Log.Succeeded;

        public string ActorName => Log.User?.FullName ?? "Hệ thống";
        public string ActorRole => Log.User == null ? "Hệ thống" : RoleText(Log.User.Role?.RoleName);
        public string ActorInitial => string.IsNullOrWhiteSpace(ActorName) ? "?" : ActorName.Trim()[..1].ToUpper();

        public string ActionKind => Log.ActionCode switch
        {
            "user.create" or "user.unlock" or "guest.create" or "guest.activate_account" or "room.create" or "approval.approve" => "Success",
            "user.lock" or "guest.delete" or "room.delete" or "approval.reject" => "Danger",
            "user.reset_password" or "guest.update" or "room.update" or "room.change_status" or "permission.update" => "Warning",
            _ => "Info"
        };

        private static string RoleText(string? roleName) => roleName switch
        {
            BusinessObjects.RoleNames.Admin => "Quản trị viên",
            BusinessObjects.RoleNames.Manager => "Quản lý",
            BusinessObjects.RoleNames.Receptionist => "Lễ tân",
            BusinessObjects.RoleNames.ServiceStaff => "Nhân viên dịch vụ",
            _ => "Không rõ vai trò",
        };
    }
}
