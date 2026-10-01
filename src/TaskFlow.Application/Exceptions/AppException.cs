namespace TaskFlow.Application.Exceptions;

/// <summary>
/// Base class for exceptions that the ExceptionMiddleware knows how to translate
/// into a specific HTTP status code. Anything else is treated as an unexpected 500.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string message) : base(message)
    {
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string resource, object key)
        : base($"{resource} with id '{key}' was not found.")
    {
    }
}

public class ConflictException : AppException
{
    public ConflictException(string message) : base(message)
    {
    }
}

public class InvalidCredentialsException : AppException
{
    public InvalidCredentialsException() : base("Invalid email or password.")
    {
    }
}

public class ForbiddenException : AppException
{
    public ForbiddenException(string message) : base(message)
    {
    }
}

/// <summary>
/// A business-rule validation failure that is not caught by DTO-level DataAnnotations/FluentValidation
/// (e.g. an invalid enum string). Maps to 400 Bad Request.
/// </summary>
public class ValidationAppException : AppException
{
    public ValidationAppException(string message) : base(message)
    {
    }
}
