namespace ERP.Application.Common.Models;

/// <summary>
/// Chuẩn hóa kết quả trả về cho nghiệp vụ ở tầng Application (thành công / thất bại kèm mã lỗi).
/// </summary>
public class Result
{
    protected Result(bool isSuccess, string? errorCode = null, string? errorMessage = null, IReadOnlyList<string>? errors = null)
    {
        if (isSuccess && !string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("Kết quả thành công không được chứa thông báo lỗi.", nameof(errorMessage));

        if (!isSuccess && string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("Kết quả thất bại bắt buộc phải có thông báo lỗi.", nameof(errorMessage));

        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        Errors = errors ?? (string.IsNullOrWhiteSpace(errorMessage) ? Array.Empty<string>() : new[] { errorMessage });
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public IReadOnlyList<string> Errors { get; }

    public static Result Success() => new(true);

    public static Result Failure(string errorMessage, string errorCode = "BUSINESS_ERROR")
        => new(false, errorCode, errorMessage);

    public static Result ValidationFailure(IReadOnlyList<string> errors)
    {
        var list = errors?.Where(e => !string.IsNullOrWhiteSpace(e)).ToArray() ?? Array.Empty<string>();
        var summary = list.Length > 0 ? string.Join("; ", list) : "Dữ liệu đầu vào không hợp lệ.";
        return new Result(false, "VALIDATION_ERROR", summary, list);
    }
}

/// <summary>
/// Chuẩn hóa kết quả trả về có kèm dữ liệu <typeparamref name="T"/>.
/// </summary>
public class Result<T> : Result
{
    private readonly T? _value;

    private Result(bool isSuccess, T? value, string? errorCode = null, string? errorMessage = null, IReadOnlyList<string>? errors = null)
        : base(isSuccess, errorCode, errorMessage, errors)
    {
        _value = value;
    }

    public T? Value => IsSuccess ? _value : default;

    public T GetRequiredValue() => IsSuccess && _value is not null
        ? _value
        : throw new InvalidOperationException(ErrorMessage ?? "Không thể đọc Value từ Result thất bại.");

    public static Result<T> Success(T value) => new(true, value);

    public new static Result<T> Failure(string errorMessage, string errorCode = "BUSINESS_ERROR")
        => new(false, default, errorCode, errorMessage);

    public new static Result<T> ValidationFailure(IReadOnlyList<string> errors)
    {
        var list = errors?.Where(e => !string.IsNullOrWhiteSpace(e)).ToArray() ?? Array.Empty<string>();
        var summary = list.Length > 0 ? string.Join("; ", list) : "Dữ liệu đầu vào không hợp lệ.";
        return new Result<T>(false, default, "VALIDATION_ERROR", summary, list);
    }
}

