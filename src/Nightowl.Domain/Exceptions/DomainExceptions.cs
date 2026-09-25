namespace Nightowl.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }

    protected DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class InvalidIsbnException : DomainException
{
    public string RawIsbn { get; }

    public InvalidIsbnException(string rawIsbn, string reason) 
        : base($"The ISBN '{rawIsbn}' is invalid: {reason}")
    {
        RawIsbn = rawIsbn;
    }
}

public class InvalidReadingProgressException : DomainException
{
    public InvalidReadingProgressException(string message) : base(message)
    {
    }
}
