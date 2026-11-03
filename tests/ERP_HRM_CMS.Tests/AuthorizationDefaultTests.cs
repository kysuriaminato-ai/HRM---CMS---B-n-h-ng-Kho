using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ERP_HRM_CMS.Tests
{
    /// <summary>
    /// Kiểm tra phân quyền mặc định: mọi endpoint không có [AllowAnonymous] phải chuyển hướng
    /// người chưa đăng nhập sang trang đăng nhập, không trả về nội dung.
    /// </summary>
    public class AuthorizationDefaultTests : IClassFixture<TestAppFactory>
    {
        private readonly TestAppFactory _factory;

        public AuthorizationDefaultTests(TestAppFactory factory) => _factory = factory;

        [Fact]
        public async Task Every_Action_Without_AllowAnonymous_Redirects_Anonymous_Requests_To_Login()
        {
            var endpoints = _factory.Services
                .GetRequiredService<IEnumerable<EndpointDataSource>>()
                .SelectMany(source => source.Endpoints)
                .OfType<RouteEndpoint>()
                .Where(endpoint => endpoint.Metadata.GetMetadata<ControllerActionDescriptor>() != null)
                .ToList();

            Assert.NotEmpty(endpoints);

            using var client = TestHelpers.NewClient(_factory);
            var checkedCount = 0;
            var failures = new List<string>();

            foreach (var endpoint in endpoints)
            {
                // Action công khai được phép trả về nội dung khi chưa đăng nhập
                if (endpoint.Metadata.GetMetadata<IAllowAnonymous>() != null) continue;

                var descriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>()!;
                string httpMethod = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.FirstOrDefault() ?? "GET";
                string path = BuildSamplePath(endpoint);

                using var request = new HttpRequestMessage(new HttpMethod(httpMethod), path);
                request.Headers.Accept.ParseAdd("text/html");
                using var response = await client.SendAsync(request);
                checkedCount++;

                string location = response.Headers.Location?.ToString() ?? string.Empty;
                bool redirectedToLogin = response.StatusCode == HttpStatusCode.Redirect
                    && location.Contains("/Auth/Login", StringComparison.OrdinalIgnoreCase);

                if (!redirectedToLogin)
                {
                    failures.Add($"{descriptor.ControllerName}.{descriptor.ActionName} [{httpMethod} {path}] -> {(int)response.StatusCode} {location}");
                }
            }

            Assert.True(checkedCount > 0, "Không có action nào được kiểm tra.");
            Assert.Empty(failures);
        }

        /// <summary>Thay tham số route bằng giá trị mẫu "1" (hoặc controller/action đã có sẵn trong route).</summary>
        private static string BuildSamplePath(RouteEndpoint endpoint)
        {
            var values = endpoint.RoutePattern.RequiredValues;
            string template = endpoint.RoutePattern.RawText ?? string.Empty;

            string path = Regex.Replace(template, @"\{(\w+)[^}]*\}", match =>
            {
                string name = match.Groups[1].Value;
                return values.TryGetValue(name, out var value) && value is string text && text.Length > 0
                    ? text
                    : "1";
            });

            return "/" + path.TrimStart('/');
        }
    }
}
