namespace FieldOps.Domain;

internal static class DomainGuard
{
    public static Guid RequiredId(Guid value, string name)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("A non-empty identifier is required.", name);

        return value;
    }

    public static string RequiredText(string? value, string name, int maxLength)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0 || normalized.Length > maxLength)
            throw new ArgumentException($"{name} must contain between 1 and {maxLength} characters.", name);

        return normalized;
    }

    public static DateTimeOffset Future(DateTimeOffset value, DateTimeOffset nowUtc, string code, string message)
    {
        if (value <= nowUtc)
            throw new WorkOrders.WorkOrderException(code, message);

        return value;
    }
}

