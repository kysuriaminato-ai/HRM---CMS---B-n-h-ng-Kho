namespace ERP.Domain.Exceptions;

/// <summary>
/// Ngoại lệ cơ sở cho các vi phạm bất biến hoặc quy tắc nghiệp vụ cốt lõi tại tầng Domain.
/// </summary>
public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string message, string code = "DOMAIN_ERROR")
        : base(message)
    {
        Code = code;
    }

    public DomainException(string message, Exception innerException, string code = "DOMAIN_ERROR")
        : base(message, innerException)
    {
        Code = code;
    }
}

