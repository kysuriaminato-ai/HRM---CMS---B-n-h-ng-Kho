using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ERP_HRM_CMS.Filters
{
    /// <summary>
    /// Chuyển ngoại lệ <see cref="UnauthorizedAccessException"/> (phát sinh khi không xác định được danh tính người dùng
    /// qua <c>ICurrentUserService.GetRequiredUserId()</c>) thành phản hồi HTTP 401 Unauthorized chuẩn xác (Khắc phục P0-04).
    /// </summary>
    public sealed class UnauthorizedAccessExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is not UnauthorizedAccessException ex)
            {
                return;
            }

            context.Result = new ObjectResult(new
            {
                success = false,
                message = string.IsNullOrWhiteSpace(ex.Message)
                    ? "Không xác định được danh tính người dùng hoặc phiên đăng nhập đã hết hạn."
                    : ex.Message
            })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };

            context.ExceptionHandled = true;
        }
    }
}

