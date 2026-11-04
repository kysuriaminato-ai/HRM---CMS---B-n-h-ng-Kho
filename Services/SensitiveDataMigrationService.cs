using System.Data;
using System.Data.Common;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace ERP_HRM_CMS.Services
{
    /// <summary>
    /// Mã hóa lại dữ liệu CCCD và số tài khoản còn ở dạng rõ (dữ liệu cũ trước khi có mã hóa).
    /// Chạy một lần bằng cờ dòng lệnh --migrate-sensitive-data; không tự chạy khi khởi động.
    /// Đọc và ghi bằng SQL trực tiếp để không đi qua AuditLog (tránh ghi giá trị rõ vào nhật ký).
    /// Chạy lại nhiều lần vẫn an toàn: giá trị đã có tiền tố "enc:v1:" bị bỏ qua.
    /// </summary>
    public static class SensitiveDataMigrationService
    {
        /// <summary>Trả về tổng số ô đã được mã hóa.</summary>
        public static async Task<int> EncryptLegacyValuesAsync(AppDbContext db, IDataProtectionProvider provider)
        {
            var protector = ProtectedStringConverter.CreateProtector(provider);
            int changed = 0;
            changed += await EncryptColumnAsync(db, "Employees", "Id", "IdentityCard", protector);
            changed += await EncryptColumnAsync(db, "EmployeeDetails", "EmployeeId", "BankAccountNumber", protector);
            return changed;
        }

        private static async Task<int> EncryptColumnAsync(
            AppDbContext db,
            string table,
            string keyColumn,
            string column,
            Microsoft.AspNetCore.DataProtection.IDataProtector protector)
        {
            var legacyRows = new List<(int Id, string Value)>();
            DbConnection connection = db.Database.GetDbConnection();
            bool openedHere = connection.State == ConnectionState.Closed;
            if (openedHere) await connection.OpenAsync();

            try
            {
                using (var select = connection.CreateCommand())
                {
                    select.CommandText =
                        $"SELECT [{keyColumn}], [{column}] FROM [{table}] " +
                        $"WHERE [{column}] IS NOT NULL AND [{column}] NOT LIKE N'{ProtectedStringConverter.Prefix}%'";
                    using var reader = await select.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        string value = reader.GetString(1);
                        if (!string.IsNullOrEmpty(value))
                        {
                            legacyRows.Add((reader.GetInt32(0), value));
                        }
                    }
                }

                foreach (var (id, value) in legacyRows)
                {
                    using var update = connection.CreateCommand();
                    update.CommandText = $"UPDATE [{table}] SET [{column}] = @value WHERE [{keyColumn}] = @id";

                    var valueParam = update.CreateParameter();
                    valueParam.ParameterName = "@value";
                    valueParam.Value = ProtectedStringConverter.ToStore(protector, value);
                    update.Parameters.Add(valueParam);

                    var idParam = update.CreateParameter();
                    idParam.ParameterName = "@id";
                    idParam.Value = id;
                    update.Parameters.Add(idParam);

                    await update.ExecuteNonQueryAsync();
                }
            }
            finally
            {
                if (openedHere) await connection.CloseAsync();
            }

            return legacyRows.Count;
        }
    }
}
