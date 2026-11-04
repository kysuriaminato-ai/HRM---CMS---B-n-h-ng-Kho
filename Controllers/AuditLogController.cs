using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Filters;

namespace ERP_HRM_CMS.Controllers
{
    [Authorize]
    [KiemTraQuyen("auth.view")]
    public class AuditLogController : Controller
    {
        private readonly AppDbContext _context;

        public AuditLogController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? entityName, string? actionType, string? username, DateTime? fromDate, DateTime? toDate, int page = 1)
        {
            int pageSize = 25;
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(entityName))
                query = query.Where(a => a.EntityName == entityName.Trim());

            if (!string.IsNullOrWhiteSpace(actionType))
                query = query.Where(a => a.Action == actionType.Trim());

            if (!string.IsNullOrWhiteSpace(username))
                query = query.Where(a => a.Username != null && a.Username.Contains(username.Trim()));

            if (fromDate.HasValue)
                query = query.Where(a => a.CreatedAt >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(a => a.CreatedAt < toDate.Value.Date.AddDays(1));

            int totalRecords = await query.CountAsync();
            var logs = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Entities = await _context.AuditLogs.Select(a => a.EntityName).Distinct().ToListAsync();
            ViewBag.TotalRecords = totalRecords;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
            ViewBag.EntityName = entityName;
            ViewBag.ActionType = actionType;
            ViewBag.Username = username;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            return View(logs);
        }

        [HttpGet]
        public async Task<IActionResult> GetDetails(long id)
        {
            var log = await _context.AuditLogs.FindAsync(id);
            if (log == null) return NotFound();
            return Json(log);
        }
    }
}
