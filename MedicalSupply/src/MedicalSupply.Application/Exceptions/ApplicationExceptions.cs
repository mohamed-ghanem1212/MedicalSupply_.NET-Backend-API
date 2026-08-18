namespace MedicalSupply.Application.Exceptions;

/// <summary>Base for Application-layer exceptions that map directly to an HTTP status code.</summary>
public abstract class AppException : Exception
{
    public abstract string Code { get; }
    public virtual IDictionary<string, object?> Details { get; } = new Dictionary<string, object?>();
    protected AppException(string message) : base(message) { }
}

public sealed class NotFoundAppException : AppException
{
    public override string Code => "NOT_FOUND";
    public NotFoundAppException(string entity, object key)
        : base($"{entity} with id '{key}' was not found.") { }
}

public sealed class ValidationAppException : AppException
{
    public override string Code => "VALIDATION_ERROR";
    public ValidationAppException(string message) : base(message) { }
    public ValidationAppException(string message, IDictionary<string, object?> details) : base(message)
    {
        foreach (var kv in details) Details[kv.Key] = kv.Value;
    }
}

public sealed class ForbiddenAppException : AppException
{
    public override string Code => "FORBIDDEN";
    public ForbiddenAppException(string message) : base(message) { }
}

public sealed class ConflictAppException : AppException
{
    public override string Code => "CONFLICT";
    public ConflictAppException(string message) : base(message) { }
}
