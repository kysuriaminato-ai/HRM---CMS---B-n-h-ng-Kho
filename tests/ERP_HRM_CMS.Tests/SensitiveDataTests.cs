using System.Data;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    public class SensitiveDataTests : IClassFixture<TestAppFactory>
    {
        private readonly TestAppFactory _factory;

        public SensitiveDataTests(TestAppFactory factory) => _factory = factory;

        private static ProtectedStringConverter NewConverter() =>
            new(ProtectedStringConverter.CreateProtector(new EphemeralDataProtectionProvider()));

        [Fact]
        public void Converter_Stores_Ciphertext_And_Reads_Back_The_Original()
        {
            var converter = NewConverter();

            string? stored = (string?)converter.ConvertToProvider("012345678901");

            Assert.NotNull(stored);
            Assert.StartsWith(ProtectedStringConverter.Prefix, stored);
            Assert.DoesNotContain("012345678901", stored);
            Assert.Equal("012345678901", converter.ConvertFromProvider(stored));
        }

        [Fact]
        public void Converter_Uses_A_New_Ciphertext_Each_Time()
        {
            var converter = NewConverter();

            Assert.NotEqual(converter.ConvertToProvider("190333888999"), converter.ConvertToProvider("190333888999"));
        }

        [Fact]
        public void Converter_Reads_Legacy_Plaintext_Unchanged()
        {
            var converter = NewConverter();

            Assert.Equal("190333888999", converter.ConvertFromProvider("190333888999"));
        }

        [Fact]
        public void Converter_Keeps_Null_And_Empty_Values()
        {
            var converter = NewConverter();

            Assert.Null(converter.ConvertToProvider(null));
            Assert.Null(converter.ConvertFromProvider(null));
            Assert.Equal(string.Empty, converter.ConvertToProvider(string.Empty));
        }

        [Fact]
        public void Mask_Shows_Only_The_Last_Four_Characters()
        {
            Assert.Equal("****8901", SensitiveDataMasker.Mask("012345678901"));
            Assert.Equal("****", SensitiveDataMasker.Mask("12"));
            Assert.Null(SensitiveDataMasker.Mask(null));
            Assert.Null(SensitiveDataMasker.Mask("   "));
        }

        [Fact]
        public async Task Identity_Card_Is_Encrypted_In_The_Database_And_Decrypted_By_EF()
        {
            const string identityCard = "079123456789";
            int employeeId;

            using (var writeScope = _factory.Services.CreateScope())
            {
                var db = writeScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var employee = await db.Employees.OrderBy(e => e.Id).FirstAsync();
                employeeId = employee.Id;
                employee.IdentityCard = identityCard;
                await db.SaveChangesAsync();
            }

            using var readScope = _factory.Services.CreateScope();
            var readDb = readScope.ServiceProvider.GetRequiredService<AppDbContext>();

            string raw = await ReadRawAsync(readDb, $"SELECT IdentityCard FROM Employees WHERE Id = {employeeId}");
            Assert.StartsWith(ProtectedStringConverter.Prefix, raw);
            Assert.DoesNotContain(identityCard, raw);

            var reloaded = await readDb.Employees.AsNoTracking().FirstAsync(e => e.Id == employeeId);
            Assert.Equal(identityCard, reloaded.IdentityCard);
        }

        private static async Task<string> ReadRawAsync(AppDbContext db, string sql)
        {
            var connection = db.Database.GetDbConnection();
            bool openedHere = connection.State == ConnectionState.Closed;
            if (openedHere) await connection.OpenAsync();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                var result = await command.ExecuteScalarAsync();
                return result as string ?? string.Empty;
            }
            finally
            {
                if (openedHere) await connection.CloseAsync();
            }
        }
    }
}
