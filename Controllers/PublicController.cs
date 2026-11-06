using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using ERP_HRM_CMS.Data;
using ERP_HRM_CMS.Helpers;
using ERP_HRM_CMS.Models.Entities;
using ERP_HRM_CMS.Models.ViewModels;
using ERP_HRM_CMS.Services.HRM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ERP_HRM_CMS.Controllers
{
    // Trang công khai: trang chủ, bài viết, giới thiệu, liên hệ, tuyển dụng công khai, sitemap.xml, robots.txt.
    [AllowAnonymous]
    public class PublicController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IRecruitmentService _recruitmentService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PublicController> _logger;

        public PublicController(
            AppDbContext context,
            IRecruitmentService recruitmentService,
            IMemoryCache cache,
            ILogger<PublicController> logger)
        {
            _context = context;
            _recruitmentService = recruitmentService;
            _cache = cache;
            _logger = logger;
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
                .Take(6)
                .ToListAsync();

            var dbProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .Take(24)
                .ToListAsync();

            ViewBag.FeaturedProducts = dbProducts;

            ViewBag.Categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.OrderIndex)
                .ToListAsync();

            // Lấy thêm tin tuyển dụng nổi bật đang mở cho trang chủ
            ViewBag.FeaturedJobs = await _context.JobPostings
                .Include(j => j.Department)
                .Where(j => !j.IsDeleted && j.Status == "Open")
                .OrderByDescending(j => j.CreatedAt)
                .Take(4)
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

            // Sinh schema NewsArticle Structured Data
            var req = HttpContext.Request;
            string baseUrl = $"{req.Scheme}://{req.Host}";
            var articleSchema = new Dictionary<string, object?>
            {
                ["@context"] = "https://schema.org",
                ["@type"] = "NewsArticle",
                ["headline"] = content.Title,
                ["description"] = content.Summary ?? content.Title,
                ["datePublished"] = content.CreatedAt.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                ["dateModified"] = content.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                ["author"] = new Dictionary<string, object?>
                {
                    ["@type"] = "Person",
                    ["name"] = content.Author?.Username ?? "Ban Biên Tập"
                },
                ["publisher"] = new Dictionary<string, object?>
                {
                    ["@type"] = "Organization",
                    ["name"] = "ERP Enterprise",
                    ["logo"] = new Dictionary<string, object?>
                    {
                        ["@type"] = "ImageObject",
                        ["url"] = $"{baseUrl}/images/logo.png"
                    }
                },
                ["mainEntityOfPage"] = $"{baseUrl}/bai-viet/{content.Slug}"
            };

            ViewBag.JsonLdSchema = JsonSerializer.Serialize(articleSchema, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

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

        #region Cổng Tuyển Dụng Công Khai (Careers Portal)

        [HttpGet]
        [Route("tuyen-dung")]
        public async Task<IActionResult> TuyenDung(string? keyword, int? departmentId, int? branchId)
        {
            var vm = await _recruitmentService.GetPublicCareersIndexAsync(keyword, departmentId, branchId);
            return View(vm);
        }

        [HttpGet]
        [Route("tuyen-dung/{slug}")]
        public async Task<IActionResult> ChiTietTuyenDung(string slug)
        {
            var vm = await _recruitmentService.GetPublicJobDetailAsync(slug);
            if (vm == null) return NotFound();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("tuyen-dung/{slug}/ung-tuyen")]
        public async Task<IActionResult> NopHoSoUngTuyen(string slug, [FromForm] PublicApplyCandidateDto model)
        {
            var job = await _recruitmentService.GetJobPostingBySlugAsync(slug);
            if (job == null || job.Status != "Open")
            {
                return NotFound("Tin tuyển dụng không tồn tại hoặc đã đóng.");
            }

            model.JobPostingId = job.Id;

            if (!ModelState.IsValid)
            {
                var detailVm = await _recruitmentService.GetPublicJobDetailAsync(slug);
                if (detailVm != null)
                {
                    detailVm.ApplyModel = model;
                    ViewBag.ErrorMessage = "Vui lòng kiểm tra lại các thông tin nộp hồ sơ.";
                    return View("ChiTietTuyenDung", detailVm);
                }
                return RedirectToAction(nameof(ChiTietTuyenDung), new { slug });
            }

            string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = await _recruitmentService.SubmitPublicApplicationAsync(model, clientIp);

            if (!result.IsSuccess)
            {
                var detailVm = await _recruitmentService.GetPublicJobDetailAsync(slug);
                if (detailVm != null)
                {
                    detailVm.ApplyModel = model;
                    ViewBag.ErrorMessage = result.ErrorMessage;
                    return View("ChiTietTuyenDung", detailVm);
                }
                return RedirectToAction(nameof(ChiTietTuyenDung), new { slug });
            }

            TempData["SuccessMessage"] = "Nộp hồ sơ ứng tuyển thành công! Bộ phận Nhân sự sẽ liên hệ với bạn trong thời gian sớm nhất.";
            return RedirectToAction(nameof(ChiTietTuyenDung), new { slug });
        }

        #endregion

        #region Technical SEO: Dynamic Sitemap.xml & Robots.txt

        [HttpGet]
        [Route("sitemap.xml")]
        [ResponseCache(Duration = 1800, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> SitemapXml()
        {
            string cacheKey = "dynamic_seo_sitemap_xml";
            if (_cache.TryGetValue(cacheKey, out string? cachedXml) && !string.IsNullOrEmpty(cachedXml))
            {
                return Content(cachedXml, "application/xml", Encoding.UTF8);
            }

            var req = HttpContext.Request;
            string baseUrl = $"{req.Scheme}://{req.Host}";

            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            var root = new XElement(ns + "urlset");

            // 1. Static Pages
            var staticPages = new (string url, string changefreq, string priority)[]
            {
                (baseUrl + "/", "daily", "1.0"),
                (baseUrl + "/Public/GioiThieu", "monthly", "0.8"),
                (baseUrl + "/Public/TinTuc", "daily", "0.8"),
                (baseUrl + "/tuyen-dung", "daily", "0.9"),
                (baseUrl + "/Public/LienHe", "monthly", "0.7")
            };

            foreach (var p in staticPages)
            {
                root.Add(new XElement(ns + "url",
                    new XElement(ns + "loc", p.url),
                    new XElement(ns + "lastmod", VietnamTime.Now.ToString("yyyy-MM-dd")),
                    new XElement(ns + "changefreq", p.changefreq),
                    new XElement(ns + "priority", p.priority)
                ));
            }

            // 2. CMS Articles
            var articles = await _context.Contents
                .Where(c => c.Status == "Published")
                .OrderByDescending(c => c.CreatedAt)
                .Take(500)
                .Select(c => new { c.Slug, Date = c.UpdatedAt })
                .ToListAsync();

            foreach (var a in articles)
            {
                root.Add(new XElement(ns + "url",
                    new XElement(ns + "loc", $"{baseUrl}/bai-viet/{a.Slug}"),
                    new XElement(ns + "lastmod", a.Date.ToString("yyyy-MM-dd")),
                    new XElement(ns + "changefreq", "weekly"),
                    new XElement(ns + "priority", "0.7")
                ));
            }

            // 3. Open Job Postings
            var jobs = await _context.JobPostings
                .Where(j => !j.IsDeleted && j.Status == "Open")
                .OrderByDescending(j => j.CreatedAt)
                .Take(200)
                .Select(j => new { j.Slug, Date = j.UpdatedAt ?? j.CreatedAt })
                .ToListAsync();

            foreach (var j in jobs)
            {
                root.Add(new XElement(ns + "url",
                    new XElement(ns + "loc", $"{baseUrl}/tuyen-dung/{j.Slug}"),
                    new XElement(ns + "lastmod", j.Date.ToString("yyyy-MM-dd")),
                    new XElement(ns + "changefreq", "daily"),
                    new XElement(ns + "priority", "0.8")
                ));
            }

            var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root);
            var stringWriter = new StringWriter();
            using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true }))
            {
                doc.WriteTo(xmlWriter);
            }

            string xmlResult = stringWriter.ToString();

            // Cache 30 minutes
            _cache.Set(cacheKey, xmlResult, TimeSpan.FromMinutes(30));

            return Content(xmlResult, "application/xml", Encoding.UTF8);
        }

        [HttpGet]
        [Route("robots.txt")]
        public IActionResult RobotsTxt()
        {
            var req = HttpContext.Request;
            string baseUrl = $"{req.Scheme}://{req.Host}";

            var sb = new StringBuilder();
            sb.AppendLine("User-agent: *");
            sb.AppendLine("Allow: /");
            sb.AppendLine("Allow: /tuyen-dung");
            sb.AppendLine("Allow: /bai-viet/");
            sb.AppendLine("Allow: /Public/");
            sb.AppendLine();
            sb.AppendLine("# Disallow Internal & Admin Endpoints");
            sb.AppendLine("Disallow: /admin");
            sb.AppendLine("Disallow: /NhanSu");
            sb.AppendLine("Disallow: /BangLuong");
            sb.AppendLine("Disallow: /ChamCong");
            sb.AppendLine("Disallow: /DonTu");
            sb.AppendLine("Disallow: /HopDong");
            sb.AppendLine("Disallow: /CongNhanVien");
            sb.AppendLine("Disallow: /TuyenDung/");
            sb.AppendLine("Disallow: /Auth/");
            sb.AppendLine("Disallow: /TaiKhoan");
            sb.AppendLine("Disallow: /PhanQuyen");
            sb.AppendLine();
            sb.AppendLine($"Sitemap: {baseUrl}/sitemap.xml");

            return Content(sb.ToString(), "text/plain", Encoding.UTF8);
        }

        #endregion
    }
}
