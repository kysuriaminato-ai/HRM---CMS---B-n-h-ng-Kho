namespace ERP_HRM_CMS.Services.Payroll
{
    /// <summary>Bộ tham số đã chọn đúng phiên bản có hiệu lực cho một kỳ lương.</summary>
    public sealed class PayrollRules
    {
        public decimal StatutoryBaseSalary { get; init; }
        public decimal PersonalDeduction { get; init; }
        public decimal DependentDeduction { get; init; }
        public InsuranceRateRule Insurance { get; init; } = null!;
        public IReadOnlyList<PitBracketOption> PitBrackets { get; init; } = Array.Empty<PitBracketOption>();
        public decimal InsuranceCapMultiplier { get; init; }
        public int LateBlockMinutes { get; init; }
        public decimal LateAmountPerBlock { get; init; }
        public decimal OvertimeMultiplier { get; init; }
        public decimal HoursPerDay { get; init; }

        /// <summary>Các quy định được dùng cho kỳ này nhưng còn cờ NeedsConfirmation (TODO).</summary>
        public IReadOnlyList<string> PendingConfirmations { get; init; } = Array.Empty<string>();
    }

    /// <summary>Chọn phiên bản tham số theo ngày hiệu lực và kiểm tra cấu hình.</summary>
    public static class PayrollRuleResolver
    {
        public static PayrollRules Resolve(PayrollOptions options, DateTime periodStart)
        {
            ArgumentNullException.ThrowIfNull(options);

            var pending = new List<string>();

            var baseSalary = Pick(options.BaseSalary, periodStart, "Lương cơ sở", pending);
            var personal = Pick(options.PersonalDeduction, periodStart, "Giảm trừ gia cảnh bản thân", pending);
            var dependent = Pick(options.DependentDeduction, periodStart, "Giảm trừ người phụ thuộc", pending);
            var rates = Pick(options.InsuranceRates, periodStart, "Tỷ lệ bảo hiểm", pending);
            var pit = Pick(options.PitSchedules, periodStart, "Biểu thuế lũy tiến TNCN", pending);
            ValidateBrackets(pit);

            AddIfPending(pending, "Trần đóng bảo hiểm", options.InsuranceCap);
            AddIfPending(pending, "Phạt đi muộn", options.LatePenalty);
            AddIfPending(pending, "Hệ số làm thêm giờ", options.Overtime);

            if (options.InsuranceCap.Multiplier <= 0m)
                throw new InvalidOperationException("Payroll:InsuranceCap:Multiplier phải lớn hơn 0.");
            if (options.LatePenalty.BlockMinutes <= 0)
                throw new InvalidOperationException("Payroll:LatePenalty:BlockMinutes phải lớn hơn 0.");
            if (options.Overtime.HoursPerDay <= 0m)
                throw new InvalidOperationException("Payroll:Overtime:HoursPerDay phải lớn hơn 0.");

            return new PayrollRules
            {
                StatutoryBaseSalary = baseSalary.Amount,
                PersonalDeduction = personal.Amount,
                DependentDeduction = dependent.Amount,
                Insurance = rates,
                PitBrackets = pit.Brackets,
                InsuranceCapMultiplier = options.InsuranceCap.Multiplier,
                LateBlockMinutes = options.LatePenalty.BlockMinutes,
                LateAmountPerBlock = options.LatePenalty.AmountPerBlock,
                OvertimeMultiplier = options.Overtime.WeekdayMultiplier,
                HoursPerDay = options.Overtime.HoursPerDay,
                PendingConfirmations = pending
            };
        }

        private static T Pick<T>(IEnumerable<T> versions, DateTime periodStart, string name, List<string> pending)
            where T : VersionedRule
        {
            var date = periodStart.Date;
            var rule = versions
                .Where(v => v.EffectiveFrom.Date <= date)
                .OrderByDescending(v => v.EffectiveFrom)
                .FirstOrDefault();

            if (rule == null)
            {
                throw new InvalidOperationException(
                    $"Chưa có \"{name}\" có hiệu lực cho kỳ tháng {periodStart:MM/yyyy}. " +
                    "Kiểm tra mục Payroll trong appsettings.json (ngày hiệu lực phải <= 01 của tháng tính lương).");
            }

            if (rule.NeedsConfirmation)
            {
                pending.Add($"{name} (hiệu lực từ {rule.EffectiveFrom:dd/MM/yyyy}): {rule.Note ?? rule.Source}");
            }

            return rule;
        }

        private static void AddIfPending(List<string> pending, string name, RuleSource rule)
        {
            if (rule.NeedsConfirmation)
            {
                pending.Add($"{name}: {rule.Note ?? rule.Source}");
            }
        }

        /// <summary>Các bậc phải tăng dần theo UpTo, và bậc cuối phải không giới hạn.</summary>
        private static void ValidateBrackets(PitScheduleRule schedule)
        {
            if (schedule.Brackets.Count == 0)
                throw new InvalidOperationException("Biểu thuế TNCN không có bậc nào.");

            decimal previous = 0m;
            for (int i = 0; i < schedule.Brackets.Count; i++)
            {
                var bracket = schedule.Brackets[i];
                bool isLast = i == schedule.Brackets.Count - 1;

                if (bracket.UpTo == null)
                {
                    if (!isLast)
                        throw new InvalidOperationException("Chỉ bậc cuối của biểu thuế TNCN được để UpTo = null.");
                    continue;
                }

                if (bracket.UpTo <= previous)
                    throw new InvalidOperationException("Các bậc của biểu thuế TNCN phải tăng dần theo UpTo.");

                previous = bracket.UpTo.Value;
            }

            if (schedule.Brackets[^1].UpTo != null)
                throw new InvalidOperationException("Bậc cuối của biểu thuế TNCN phải có UpTo = null.");
        }
    }
}
