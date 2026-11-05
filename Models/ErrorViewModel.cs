namespace ERP_HRM_CMS.Models;

public class ErrorViewModel
{
    public int StatusCode { get; set; } = 500;

    public string? RequestId { get; set; }

    public string? OriginalPath { get; set; }

    public string? ReturnUrl { get; set; }

    public string? Title { get; set; }

    public string? Message { get; set; }

    public string? IconClass { get; set; }

    public string? Variant { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
