namespace ERP_HRM_CMS.Services.Security
{
    /// <summary>
    /// Quy tắc khi gán hoặc thu hồi quyền:
    /// - Người thao tác chỉ được gán/thu hồi quyền mà họ đang có (quyền hiệu lực của chính họ).
    /// - Nhóm quyền "auth.*" chỉ SuperAdmin được cấp.
    /// Hàm thuần (không truy cập database) để dễ kiểm thử.
    /// </summary>
    public static class PermissionDelegationGuard
    {
        public const string TienToQuyenNhayCam = "auth.";
        public const string ThongBaoKhongDuQuyen = "Bạn chỉ được gán hoặc thu hồi quyền mà chính bạn đang có.";
        public const string ThongBaoAuthChiSuperAdmin = "Chỉ SuperAdmin mới được cấp hoặc thu hồi quyền nhóm 'auth.*'.";

        /// <param name="thayDoi">Các quyền bị thêm hoặc bị bỏ trong lần lưu này (Id, Code).</param>
        /// <param name="quyenHienCo">Tập mã quyền hiệu lực của người thao tác.</param>
        /// <param name="laSuperAdmin">Người thao tác có phải SuperAdmin (đã kiểm tra từ database).</param>
        /// <returns>Thông báo lỗi, hoặc null nếu được phép.</returns>
        public static string? KiemTra(
            IEnumerable<(int Id, string Code)> thayDoi,
            IReadOnlySet<string> quyenHienCo,
            bool laSuperAdmin)
        {
            foreach (var (_, code) in thayDoi)
            {
                if (laSuperAdmin)
                {
                    continue;
                }

                if (code.StartsWith(TienToQuyenNhayCam, StringComparison.OrdinalIgnoreCase))
                {
                    return ThongBaoAuthChiSuperAdmin;
                }

                if (!quyenHienCo.Contains(code))
                {
                    return ThongBaoKhongDuQuyen;
                }
            }

            return null;
        }
    }
}
