namespace SportFlow.Shared.Exceptions;

public class SportFlowException : Exception
{
    public string ErrorCode { get; }

    public SportFlowException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}

public class NotFoundException : SportFlowException
{
    public NotFoundException(string resource, object id)
        : base("NOT_FOUND", $"{resource} with id '{id}' was not found.") { }
}

public class ForbiddenException : SportFlowException
{
    public ForbiddenException(string message = "You do not have permission to perform this action.")
        : base("FORBIDDEN", message) { }
}

public class ValidationException : SportFlowException
{
    public IReadOnlyList<ValidationError> Errors { get; }

    public ValidationException(IReadOnlyList<ValidationError> errors)
        : base("VALIDATION_ERROR", "One or more validation errors occurred.")
    {
        Errors = errors;
    }
}

public record ValidationError(string Field, string Message, string Code);
