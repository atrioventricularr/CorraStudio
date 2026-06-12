using FluentValidation;
using CorraStudio.Application.Commands.Session;

namespace CorraStudio.Application.Validators;

public class CreateSessionCommandValidator : AbstractValidator<CreateSessionCommand>
{
    public CreateSessionCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId is required");

        RuleFor(x => x.SessionCode)
            .NotEmpty().WithMessage("SessionCode is required")
            .MaximumLength(50).WithMessage("SessionCode must not exceed 50 characters")
            .Matches("^[a-zA-Z0-9-]+$").WithMessage("SessionCode can only contain letters, numbers and hyphens");
    }
}

public class StartSessionCommandValidator : AbstractValidator<StartSessionCommand>
{
    public StartSessionCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("SessionId is required");
    }
}

public class CancelSessionCommandValidator : AbstractValidator<CancelSessionCommand>
{
    public CancelSessionCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("SessionId is required");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters");
    }
}
