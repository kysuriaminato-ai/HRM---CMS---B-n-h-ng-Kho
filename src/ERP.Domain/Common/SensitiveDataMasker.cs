namespace ERP_HRM_CMS.Helpers
{
    /// <summary>Che số khi hiển thị: "****1234". Giữ nguyên null; giá trị quá ngắn chỉ hiện "****".</summary>
    public static class SensitiveDataMasker
    {
        public static string? Mask(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var trimmed = value.Trim();
            return trimmed.Length <= 4 ? "****" : "****" + trimmed.Substring(trimmed.Length - 4);
        }
    }
}

