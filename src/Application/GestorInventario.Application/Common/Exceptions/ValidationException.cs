using FluentValidation.Results;

namespace GestorInventario.Application.Common.Exceptions;

public class ValidationException : Exception
{
    public ValidationException()
        : base("One or more validation failures have occurred.")
    {
        Errors = Array.Empty<ValidationError>();
    }

    public ValidationException(string message, string? propertyName = null)
        : base(message)
    {
        Errors = new[]
        {
            new ValidationError(propertyName ?? string.Empty, message)
        };
    }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        Errors = failures
            .Where(failure => failure is not null)
            .Select(failure => new ValidationError(failure.PropertyName, failure.ErrorMessage))
            .ToArray();
    }

    public IReadOnlyCollection<ValidationError> Errors { get; private set; }

    public record ValidationError(string PropertyName, string ErrorMessage);
}
