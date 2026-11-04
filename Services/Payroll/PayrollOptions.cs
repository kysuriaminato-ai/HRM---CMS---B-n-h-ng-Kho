namespace ERP_HRM_CMS.Services.Payroll
{
    /// <summary>
    /// Tham số tính lương, đọc từ mục "Payroll" trong appsettings.json.
    ///
    /// Quy tắc:
    /// - Mỗi quy định có <see cref="RuleSource.Source"/> (nguồn văn bản). Quy định có hiệu lực theo thời gian
    ///   được khai báo thành nhiều phiên bản (<see cref="VersionedRule.EffectiveFrom"/>). Kỳ lương dùng phiên bản
    ///   mới nhất có ngày hiệu lực &lt;= ngày 01 của tháng tính lương.
    /// - <see cref="RuleSource.NeedsConfirmation"/> = true nghĩa là mục TODO: chưa đối chiếu đủ nguồn hoặc đang chờ
    ///   chính sách công ty. Hệ thống vẫn tính, nhưng ghi cảnh báo vào phiếu lương.
    /// - Không đặt giá trị suy đoán. Nếu thiếu phiên bản có hiệu lực cho một kỳ, việc tính lương sẽ báo lỗi.
    /// </summary>
    public sealed class PayrollOptions
    {
        public const string SectionName = "Payroll";

        /// <summary>Lương cơ sở (đồng/tháng). Dùng làm mốc trần đóng bảo hiểm.</summary>
        public List<MoneyRule> BaseSalary { get; set; } = new();

        /// <summary>Giảm trừ gia cảnh cho bản thân người nộp thuế (đồng/tháng).</summary>
        public List<MoneyRule> PersonalDeduction { get; set; } = new();

        /// <summary>Giảm trừ cho mỗi người phụ thuộc (đồng/tháng).</summary>
        public List<MoneyRule> DependentDeduction { get; set; } = new();

        /// <summary>Tỷ lệ BHXH, BHYT, BHTN, TNLĐ-BNN của người lao động và người sử dụng lao động (dạng số thập phân, 0,08 = 8%).</summary>
        public List<InsuranceRateRule> InsuranceRates { get; set; } = new();

        /// <summary>Biểu thuế lũy tiến từng phần TNCN (tháng).</summary>
        public List<PitScheduleRule> PitSchedules { get; set; } = new();

        /// <summary>Trần đóng bảo hiểm = hệ số × lương cơ sở.</summary>
        public InsuranceCapRule InsuranceCap { get; set; } = new();

        /// <summary>Phạt đi muộn: mỗi block số phút trễ bị phạt một số tiền.</summary>
        public LatePenaltyRule LatePenalty { get; set; } = new();

        /// <summary>Hệ số làm thêm giờ.</summary>
        public OvertimeRule Overtime { get; set; } = new();
    }

    /// <summary>Nguồn và trạng thái xác nhận của một quy định.</summary>
    public abstract class RuleSource
    {
        /// <summary>Nguồn văn bản hoặc căn cứ chính sách.</summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>true = TODO, cần xác nhận trước khi dùng thật.</summary>
        public bool NeedsConfirmation { get; set; }

        /// <summary>Ghi chú thêm, thường là phần TODO cần làm rõ.</summary>
        public string? Note { get; set; }
    }

    /// <summary>Quy định có nhiều phiên bản theo ngày hiệu lực.</summary>
    public abstract class VersionedRule : RuleSource
    {
        public DateTime EffectiveFrom { get; set; }
    }

    public sealed class MoneyRule : VersionedRule
    {
        public decimal Amount { get; set; }
    }

    public sealed class InsuranceRateRule : VersionedRule
    {
        // Người lao động
        public decimal EmployeeSocial { get; set; }        // BHXH: hưu trí, tử tuất
        public decimal EmployeeHealth { get; set; }        // BHYT
        public decimal EmployeeUnemployment { get; set; }  // BHTN

        // Người sử dụng lao động
        public decimal EmployerPension { get; set; }          // BHXH: hưu trí, tử tuất
        public decimal EmployerSickMaternity { get; set; }    // BHXH: ốm đau, thai sản
        public decimal EmployerHealth { get; set; }           // BHYT
        public decimal EmployerUnemployment { get; set; }     // BHTN
        public decimal EmployerAccident { get; set; }         // TNLĐ-BNN
    }

    public sealed class PitScheduleRule : VersionedRule
    {
        /// <summary>Các bậc theo thứ tự tăng dần. Bậc cuối có UpTo = null (không giới hạn).</summary>
        public List<PitBracketOption> Brackets { get; set; } = new();
    }

    public sealed class PitBracketOption
    {
        /// <summary>Cận trên của bậc (đồng/tháng). Null = không giới hạn.</summary>
        public decimal? UpTo { get; set; }

        /// <summary>Thuế suất của bậc (0,05 = 5%).</summary>
        public decimal Rate { get; set; }
    }

    public sealed class InsuranceCapRule : RuleSource
    {
        /// <summary>Số lần lương cơ sở làm trần đóng bảo hiểm.</summary>
        public decimal Multiplier { get; set; } = 20m;
    }

    public sealed class LatePenaltyRule : RuleSource
    {
        /// <summary>Số phút trễ tạo thành một block bị phạt.</summary>
        public int BlockMinutes { get; set; } = 15;

        /// <summary>Số tiền phạt cho mỗi block (đồng).</summary>
        public decimal AmountPerBlock { get; set; } = 50000m;
    }

    public sealed class OvertimeRule : RuleSource
    {
        /// <summary>Hệ số làm thêm giờ ngày thường (1,5 = 150%).</summary>
        public decimal WeekdayMultiplier { get; set; } = 1.5m;

        /// <summary>Số giờ của một ngày công, dùng để đổi lương ngày thành lương giờ.</summary>
        public decimal HoursPerDay { get; set; } = 8m;
    }
}
