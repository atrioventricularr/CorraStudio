using FluentValidation;
using CorraStudio.Application.Commands.Layout;

namespace CorraStudio.Application.Validators;

public class CreateLayoutCommandValidator : AbstractValidator<CreateLayoutCommand>
{
    public CreateLayoutCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Width)
            .GreaterThan(0).WithMessage("Width must be greater than 0")
            .LessThanOrEqualTo(5000).WithMessage("Width must not exceed 5000");

        RuleFor(x => x.Height)
            .GreaterThan(0).WithMessage("Height must be greater than 0")
            .LessThanOrEqualTo(5000).WithMessage("Height must not exceed 5000");

        RuleFor(x => x.ConfigJson)
            .NotEmpty().WithMessage("ConfigJson is required");
    }
}

public class UpdateLayoutCommandValidator : AbstractValidator<UpdateLayoutCommand>
{
    public UpdateLayoutCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Width)
            .GreaterThan(0).WithMessage("Width must be greater than 0")
            .LessThanOrEqualTo(5000).WithMessage("Width must not exceed 5000");

        RuleFor(x => x.Height)
            .GreaterThan(0).WithMessage("Height must be greater than 0")
            .LessThanOrEqualTo(5000).WithMessage("Height must not exceed 5000");
    }
}
