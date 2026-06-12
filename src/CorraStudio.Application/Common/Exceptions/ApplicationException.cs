namespace CorraStudio.Application.Common.Exceptions;

public class ApplicationException : Exception
{
    public ApplicationException() : base() { }
    
    public ApplicationException(string message) : base(message) { }
    
    public ApplicationException(string message, Exception innerException) : base(message, innerException) { }
}

public class NotFoundException : ApplicationException
{
    public NotFoundException(string entityName, object id) 
        : base($"Entity '{entityName}' with id '{id}' was not found") { }
}

public class BusinessRuleException : ApplicationException
{
    public BusinessRuleException(string message) : base(message) { }
}

public class UnauthorizedException : ApplicationException
{
    public UnauthorizedException(string message) : base(message) { }
}
