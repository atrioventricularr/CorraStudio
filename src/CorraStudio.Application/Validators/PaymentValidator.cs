using FluentValidation;
using CorraStudio.Application.Commands.Payment;
using CorraStudio.Domain.Enums;

namespace CorraStudio.Application.Validators;

public class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("SessionId is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0");

        RuleFor(x => x.Method)
            .NotEmpty().WithMessage("Method is required")
            .Must(m => Enum.TryParse<PaymentMethod>(m, out _))
            .WithMessage("Invalid payment method");
    }
}

public class ProcessPaymentCommandValidator : AbstractValidator<ProcessPaymentCommand>
{
    public ProcessPaymentCommandValidator()
    {
        RuleFor(x => x.PaymentId)
            .NotEmpty().WithMessage("PaymentId is required");

        When(x => x.IsSuccess, () =>
        {
            RuleFor(x => x.ProviderTransactionId)
                .NotEmpty().WithMessage("ProviderTransactionId is required when payment is successful");
        });

        When(x => !x.IsSuccess, () =>
        {
            RuleFor(x => x.FailureReason)
                .NotEmpty().WithMessage("FailureReason is required when payment failed");
        });
    }
}
