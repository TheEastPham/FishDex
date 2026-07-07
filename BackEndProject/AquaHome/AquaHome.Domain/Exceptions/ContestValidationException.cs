namespace AquaHome.Domain.Exceptions;

/// <summary>Ném khi submit video contest không hợp lệ (thời lượng, format...) — API filter map thành HTTP 422.</summary>
public class ContestValidationException(string message) : Exception(message);
