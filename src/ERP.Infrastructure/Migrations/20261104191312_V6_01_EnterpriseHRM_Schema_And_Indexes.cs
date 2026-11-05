using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP_HRM_CMS.Migrations
{
    /// <inheritdoc />
    public partial class V6_01_EnterpriseHRM_Schema_And_Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Branches_BranchId",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_Branches_BranchId",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Employees_DepartmentId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Candidates_JobPostingId",
                table: "Candidates");

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "JobPostings",
                type: "nvarchar(350)",
                maxLength: 350,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Employees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Employees",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AttendanceAdjustmentRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    WorkDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ShiftId = table.Column<int>(type: "int", nullable: true),
                    AdjustmentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActualInTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    ActualOutTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AttachmentUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ApprovedById = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovalNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceAdjustmentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceAdjustmentRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceAdjustmentRequests_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceAdjustmentRequests_Users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceLocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LockedById = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UnlockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UnlockedById = table.Column<int>(type: "int", nullable: true),
                    UnlockReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceLocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceLocks_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceLocks_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceLocks_Users_LockedById",
                        column: x => x.LockedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceLocks_Users_UnlockedById",
                        column: x => x.UnlockedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanyAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssetName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AssetType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Specification = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PurchasePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WarrantyExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyAssets_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeLeaveBalances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    LeaveTypeId = table.Column<int>(type: "int", nullable: false),
                    StandardDays = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SeniorityBonusDays = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CarriedOverDays = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    UsedDays = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    RemainingDays = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ExpiredCarryOverDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeLeaveBalances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeLeaveBalances_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeLeaveBalances_LeaveTypes_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "LeaveTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HrDecisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DecisionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    DecisionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SignDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OldDepartmentId = table.Column<int>(type: "int", nullable: true),
                    NewDepartmentId = table.Column<int>(type: "int", nullable: true),
                    OldPositionId = table.Column<int>(type: "int", nullable: true),
                    NewPositionId = table.Column<int>(type: "int", nullable: true),
                    OldSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NewSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AttachmentUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ApprovedById = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SignerName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SignerTitle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HrDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HrDecisions_Departments_NewDepartmentId",
                        column: x => x.NewDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HrDecisions_Departments_OldDepartmentId",
                        column: x => x.OldDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HrDecisions_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HrDecisions_Positions_NewPositionId",
                        column: x => x.NewPositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HrDecisions_Positions_OldPositionId",
                        column: x => x.OldPositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HrDecisions_Users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InterviewSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CandidateId = table.Column<int>(type: "int", nullable: false),
                    RoundNumber = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ScheduledTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    InterviewType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LocationOrMeetingLink = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InterviewerEmployeeId = table.Column<int>(type: "int", nullable: true),
                    InterviewerUserId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    Feedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Result = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewSchedules_Candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "Candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterviewSchedules_Employees_InterviewerEmployeeId",
                        column: x => x.InterviewerEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InterviewSchedules_Users_InterviewerUserId",
                        column: x => x.InterviewerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingChecklists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TargetCompletionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssignedHrId = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingChecklists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingChecklists_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnboardingChecklists_Users_AssignedHrId",
                        column: x => x.AssignedHrId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeAssetAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConditionOnAssign = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    HandoverDocumentUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReturnedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConditionOnReturn = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ReturnHandoverDocumentUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssignedById = table.Column<int>(type: "int", nullable: true),
                    ReturnedById = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAssetAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAssetAssignments_CompanyAssets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "CompanyAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeAssetAssignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeAssetAssignments_Users_AssignedById",
                        column: x => x.AssignedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeAssetAssignments_Users_ReturnedById",
                        column: x => x.ReturnedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChecklistId = table.Column<int>(type: "int", nullable: false),
                    TaskName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    DepartmentRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssignedToUserId = table.Column<int>(type: "int", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingTasks_OnboardingChecklists_ChecklistId",
                        column: x => x.ChecklistId,
                        principalTable: "OnboardingChecklists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OnboardingTasks_Users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_Status_FromDate_ToDate",
                table: "LeaveRequests",
                columns: new[] { "Status", "FromDate", "ToDate" });

            migrationBuilder.CreateIndex(
                name: "IX_JobPostings_Slug",
                table: "JobPostings",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DepartmentId_Status_IsDeleted",
                table: "Employees",
                columns: new[] { "DepartmentId", "Status", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Contents_Slug",
                table: "Contents",
                column: "Slug");

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_JobPostingId_Status",
                table: "Candidates",
                columns: new[] { "JobPostingId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAdjustmentRequests_ApprovedById",
                table: "AttendanceAdjustmentRequests",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAdjustmentRequests_EmployeeId_WorkDate",
                table: "AttendanceAdjustmentRequests",
                columns: new[] { "EmployeeId", "WorkDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAdjustmentRequests_RequestCode",
                table: "AttendanceAdjustmentRequests",
                column: "RequestCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAdjustmentRequests_ShiftId",
                table: "AttendanceAdjustmentRequests",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAdjustmentRequests_Status_WorkDate",
                table: "AttendanceAdjustmentRequests",
                columns: new[] { "Status", "WorkDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLocks_BranchId",
                table: "AttendanceLocks",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLocks_DepartmentId",
                table: "AttendanceLocks",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLocks_LockedById",
                table: "AttendanceLocks",
                column: "LockedById");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLocks_UnlockedById",
                table: "AttendanceLocks",
                column: "UnlockedById");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLocks_Year_Month_BranchId_DepartmentId",
                table: "AttendanceLocks",
                columns: new[] { "Year", "Month", "BranchId", "DepartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyAssets_AssetCode",
                table: "CompanyAssets",
                column: "AssetCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyAssets_BranchId_Status",
                table: "CompanyAssets",
                columns: new[] { "BranchId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAssetAssignments_AssetId_Status",
                table: "EmployeeAssetAssignments",
                columns: new[] { "AssetId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAssetAssignments_AssignedById",
                table: "EmployeeAssetAssignments",
                column: "AssignedById");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAssetAssignments_EmployeeId_Status",
                table: "EmployeeAssetAssignments",
                columns: new[] { "EmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAssetAssignments_ReturnedById",
                table: "EmployeeAssetAssignments",
                column: "ReturnedById");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLeaveBalances_EmployeeId_Year_LeaveTypeId",
                table: "EmployeeLeaveBalances",
                columns: new[] { "EmployeeId", "Year", "LeaveTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLeaveBalances_LeaveTypeId",
                table: "EmployeeLeaveBalances",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_HrDecisions_ApprovedById",
                table: "HrDecisions",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_HrDecisions_DecisionNumber",
                table: "HrDecisions",
                column: "DecisionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HrDecisions_DecisionType_EffectiveDate",
                table: "HrDecisions",
                columns: new[] { "DecisionType", "EffectiveDate" });

            migrationBuilder.CreateIndex(
                name: "IX_HrDecisions_EmployeeId_EffectiveDate",
                table: "HrDecisions",
                columns: new[] { "EmployeeId", "EffectiveDate" });

            migrationBuilder.CreateIndex(
                name: "IX_HrDecisions_NewDepartmentId",
                table: "HrDecisions",
                column: "NewDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_HrDecisions_NewPositionId",
                table: "HrDecisions",
                column: "NewPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_HrDecisions_OldDepartmentId",
                table: "HrDecisions",
                column: "OldDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_HrDecisions_OldPositionId",
                table: "HrDecisions",
                column: "OldPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSchedules_CandidateId_RoundNumber",
                table: "InterviewSchedules",
                columns: new[] { "CandidateId", "RoundNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSchedules_InterviewerEmployeeId",
                table: "InterviewSchedules",
                column: "InterviewerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSchedules_InterviewerUserId",
                table: "InterviewSchedules",
                column: "InterviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSchedules_ScheduledTime_Status",
                table: "InterviewSchedules",
                columns: new[] { "ScheduledTime", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingChecklists_AssignedHrId",
                table: "OnboardingChecklists",
                column: "AssignedHrId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingChecklists_EmployeeId_Type",
                table: "OnboardingChecklists",
                columns: new[] { "EmployeeId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTasks_AssignedToUserId",
                table: "OnboardingTasks",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTasks_ChecklistId_OrderIndex",
                table: "OnboardingTasks",
                columns: new[] { "ChecklistId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTasks_DepartmentRole_IsCompleted",
                table: "OnboardingTasks",
                columns: new[] { "DepartmentRole", "IsCompleted" });

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Branches_BranchId",
                table: "Departments",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_Branches_BranchId",
                table: "Warehouses",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Branches_BranchId",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_Branches_BranchId",
                table: "Warehouses");

            migrationBuilder.DropTable(
                name: "AttendanceAdjustmentRequests");

            migrationBuilder.DropTable(
                name: "AttendanceLocks");

            migrationBuilder.DropTable(
                name: "EmployeeAssetAssignments");

            migrationBuilder.DropTable(
                name: "EmployeeLeaveBalances");

            migrationBuilder.DropTable(
                name: "HrDecisions");

            migrationBuilder.DropTable(
                name: "InterviewSchedules");

            migrationBuilder.DropTable(
                name: "OnboardingTasks");

            migrationBuilder.DropTable(
                name: "CompanyAssets");

            migrationBuilder.DropTable(
                name: "OnboardingChecklists");

            migrationBuilder.DropIndex(
                name: "IX_LeaveRequests_Status_FromDate_ToDate",
                table: "LeaveRequests");

            migrationBuilder.DropIndex(
                name: "IX_JobPostings_Slug",
                table: "JobPostings");

            migrationBuilder.DropIndex(
                name: "IX_Employees_DepartmentId_Status_IsDeleted",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Contents_Slug",
                table: "Contents");

            migrationBuilder.DropIndex(
                name: "IX_Candidates_JobPostingId_Status",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Employees");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DepartmentId",
                table: "Employees",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_JobPostingId",
                table: "Candidates",
                column: "JobPostingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Branches_BranchId",
                table: "Departments",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_Branches_BranchId",
                table: "Warehouses",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
