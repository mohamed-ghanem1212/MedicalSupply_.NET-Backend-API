namespace MedicalSupply.Domain.Exceptions;

// A small, reusable set of exceptions instead of one class per error type.
// Each one maps to an HTTP status code in the API's exception middleware.

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}
