using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP_HRM_CMS.Data;

using ERP_HRM_CMS.Helpers;

namespace ERP_HRM_CMS.Controllers
{
    public class PublicController : Controller
    {
        private readonly AppDbContext _context;

        public PublicController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("/")]
        public async Task<IActionResult> Index()
        {
            // Đọc các khối đang kích hoạt theo thứ tự OrderIndex
            var activeBlocks = await _context.SiteLayouts
                .Where(b => b.IsActive)
                .OrderBy(b => b.OrderIndex)
                .ToListAsync();

            ViewBag.LatestPosts = await _context.Contents
                .Include(c => c.Author)
                .Where(c => c.Status == "Published")
                .OrderByDescending(c => c.CreatedAt)
                .Take(4)
                .ToListAsync();

            ViewBag.FeaturedProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .Take(6)
                .ToListAsync();

            return View(activeBlocks);
        }

        [HttpGet]
        [Route("bai-viet/{slug}")]
        public async Task<IActionResult> ChiTiet(string slug)
        {
            var content = await _context.Contents
                .Include(c => c.Author)
                .FirstOrDefaultAsync(c => c.Slug == slug && c.Status == "Published");

            if (content == null) return NotFound();

            content.ViewCount += 1;
            await _context.SaveChangesAsync();

            // Nội dung CMS do người biên tập nhập: làm sạch trước khi hiển thị công khai
            content.BodyHtml = HtmlSanitizerHelper.Sanitize(content.BodyHtml);

            return View(content);
        }

        [HttpGet]
        public async Task<IActionResult> TinTuc(string? keyword)
        {
            var query = _context.Contents
                .Include(c => c.Author)
                .Where(c => c.Status == "Published")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim().ToLower();
                query = query.Where(c => c.Title.ToLower().Contains(kw) || (c.Summary != null && c.Summary.ToLower().Contains(kw)));
            }

            var list = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> GioiThieu()
        {
            var company = await _context.Companies.FirstOrDefaultAsync();
            return View(company);
        }

        [HttpGet]
        public async Task<IActionResult> LienHe()
        {
            var company = await _context.Companies.Include(c => c.Branches).FirstOrDefaultAsync();
            return View(company);
        }
    }
}
