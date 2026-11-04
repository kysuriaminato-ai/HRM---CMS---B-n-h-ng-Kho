using ERP_HRM_CMS.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_HRM_CMS.Models.Entities
{
    [Table("Companies")]
    public class Company
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;
        [Required, MaxLength(250)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(50)]
        public string? TaxCode { get; set; }
        [MaxLength(20)]
        public string? Phone { get; set; }
        [MaxLength(100)]
        public string? Email { get; set; }
        [MaxLength(300)]
        public string? Address { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        public virtual ICollection<Branch> Branches { get; set; } = new List<Branch>();
    }

    [Table("Branches")]
    public class Branch
    {
        [Key]
        public int Id { get; set; }
        public int CompanyId { get; set; }
        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(20)]
        public string? Phone { get; set; }
        [MaxLength(300)]
        public string? Address { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        [ForeignKey("CompanyId")]
        public virtual Company Company { get; set; } = null!;
        public virtual ICollection<Department> Departments { get; set; } = new List<Department>();
    }

    [Table("Departments")]
    public class Department
    {
        [Key]
        public int Id { get; set; }
        public int BranchId { get; set; }
        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        [ForeignKey("BranchId")]
        public virtual Branch Branch { get; set; } = null!;
        [ForeignKey("ParentId")]
        public virtual Department? Parent { get; set; }
        public virtual ICollection<Department> Children { get; set; } = new List<Department>();
        public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }

    [Table("Positions")]
    public class Position
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseSalary { get; set; } = 0;
        [Column(TypeName = "decimal(5,2)")]
        public decimal AllowanceRate { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }

    [Table("Employees")]
    public class Employee
    {
        [Key]
        public int Id { get; set; }
        public int DepartmentId { get; set; }
        public int PositionId { get; set; }
        [Required, MaxLength(50)]
        public string EmployeeCode { get; set; } = string.Empty;
        [Required, MaxLength(150)]
        public string FullName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        [MaxLength(10)]
        public string? Gender { get; set; }
        [Required, MaxLength(150)]
        public string Email { get; set; } = string.Empty;
        [MaxLength(20)]
        public string? Phone { get; set; }
        [MaxLength(30)]
        public string? IdentityCard { get; set; }
        [MaxLength(500)]
        public string? AvatarUrl { get; set; }
        [Required, MaxLength(50)]
        public string Status { get; set; } = "Active";
        public DateTime HireDate { get; set; } = VietnamTime.Today;
        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; } = null!;
        [ForeignKey("PositionId")]
        public virtual Position Position { get; set; } = null!;
        public virtual EmployeeDetail? EmployeeDetail { get; set; }
        public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();
        public virtual ICollection<AttendanceDaily> AttendanceDailies { get; set; } = new List<AttendanceDaily>();

        // Phiên 5: Hồ sơ mở rộng
        public virtual ICollection<EmployeeRelative> Relatives { get; set; } = new List<EmployeeRelative>();
        public virtual ICollection<EmployeeSkill> Skills { get; set; } = new List<EmployeeSkill>();
        public virtual ICollection<EmployeeCertificate> Certificates { get; set; } = new List<EmployeeCertificate>();
        public virtual ICollection<EmployeeDocument> Documents { get; set; } = new List<EmployeeDocument>();
        public virtual ICollection<EmployeeChangeHistory> ChangeHistories { get; set; } = new List<EmployeeChangeHistory>();
    }

    [Table("EmployeeDetails")]
    public class EmployeeDetail
    {
        [Key]
        public int EmployeeId { get; set; }
        [MaxLength(150)]
        public string? EmergencyContactName { get; set; }
        [MaxLength(20)]
        public string? EmergencyContactPhone { get; set; }
        [MaxLength(50)]
        public string? BankAccountNumber { get; set; }
        [MaxLength(150)]
        public string? BankName { get; set; }
        [MaxLength(150)]
        public string? BankBranch { get; set; }
        [MaxLength(300)]
        public string? CurrentAddress { get; set; }
        [MaxLength(300)]
        public string? PermanentAddress { get; set; }
        [MaxLength(50)]
        public string? TaxIdentificationNumber { get; set; }
        [MaxLength(50)]
        public string? SocialInsuranceNumber { get; set; }
        [MaxLength(100)]
        public string? EducationLevel { get; set; }

        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; } = null!;
    }

    [Table("Contracts")]
    public class Contract
    {
        [Key]
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        [Required, MaxLength(100)]
        public string ContractNumber { get; set; } = string.Empty;
        [Required, MaxLength(100)]
        public string ContractType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal BasicSalary { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")]
        public decimal InsuranceSalary { get; set; } = 0;
        [Required, MaxLength(50)]
        public string Status { get; set; } = "Active";
        public DateTime SignDate { get; set; }
        [MaxLength(500)]
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; } = null!;
    }

    [Table("Shifts")]
    public class Shift
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public TimeSpan? BreakStartTime { get; set; }
        public TimeSpan? BreakEndTime { get; set; }
        [Column(TypeName = "decimal(4,2)")]
        public decimal WorkHours { get; set; } = 8.0m;
        public bool IsActive { get; set; } = true;
    }

    [Table("AttendanceDailies")]
    public class AttendanceDaily
    {
        [Key]
        public long Id { get; set; }
        public int EmployeeId { get; set; }
        public int ShiftId { get; set; }
        public DateTime WorkDate { get; set; }
        public TimeSpan? FirstInTime { get; set; }
        public TimeSpan? LastOutTime { get; set; }
        public int LateMinutes { get; set; } = 0;
        public int EarlyMinutes { get; set; } = 0;
        [Column(TypeName = "decimal(4,2)")]
        public decimal WorkHourCount { get; set; } = 0;
        [Column(TypeName = "decimal(4,2)")]
        public decimal OvertimeHours { get; set; } = 0;
        [Required, MaxLength(50)]
        public string AttendanceStatus { get; set; } = "Present";
        [MaxLength(250)]
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = VietnamTime.Now;

        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; } = null!;
        [ForeignKey("ShiftId")]
        public virtual Shift Shift { get; set; } = null!;
    }
}
