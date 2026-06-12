using FluentValidation;
using MediatR;
using CorraStudio.Application.DTOs;

namespace CorraStudio.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

        if (failures.Count != 0)
        {
            var errors = failures.Select(f => f.ErrorMessage).ToList();
            var responseType = typeof(TResponse);
            
            if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(ApiResponse<>))
            {
                var result = Activator.CreateInstance<TResponse>();
                var method = responseType.GetMethod("Fail");
                if (method != null)
                {
                    var failResult = method.Invoke(null, new object[] { "Validation failed", errors });
                    return (TResponse)failResult!;
                }
            }
            
            throw new ValidationException(failures);
        }

        return await next();
    }
}
