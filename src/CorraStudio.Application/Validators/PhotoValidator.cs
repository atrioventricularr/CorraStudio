using FluentValidation;
using CorraStudio.Application.Commands.Photo;

namespace CorraStudio.Application.Validators;

public class CapturePhotoCommandValidator : AbstractValidator<CapturePhotoCommand>
{
    public CapturePhotoCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("SessionId is required");

        RuleFor(x => x.ImageData)
            .NotEmpty().WithMessage("ImageData is required")
            .Must(x => x.Length <= 50 * 1024 * 1024).WithMessage("Image size must not exceed 50MB");

        RuleFor(x => x.Width)
            .GreaterThan(0).WithMessage("Width must be greater than 0");

        RuleFor(x => x.Height)
            .GreaterThan(0).WithMessage("Height must be greater than 0");
    }
}

public class SelectPhotosCommandValidator : AbstractValidator<SelectPhotosCommand>
{
    public SelectPhotosCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("SessionId is required");

        RuleFor(x => x.PhotoIds)
            .NotNull().WithMessage("PhotoIds cannot be null");
    }
}
