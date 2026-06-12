using CorraStudio.Application.DTOs;
using CorraStudio.Domain.Entities;
using CorraStudio.Domain.ValueObjects;

namespace CorraStudio.Application.Mappers;

public static class MappingProfile
{
    public static SessionDto ToDto(this Session session)
    {
        return new SessionDto
        {
            Id = session.Id,
            TenantId = session.TenantId ?? Guid.Empty,
            SessionCode = session.SessionCode,
            LayoutId = session.LayoutId,
            TemplateId = session.TemplateId,
            Status = session.Status.ToString(),
            StartedAt = session.StartedAt,
            CompletedAt = session.CompletedAt,
            PhotoCount = session.PhotoCount,
            TotalAmount = session.TotalAmount?.Amount,
            Currency = session.TotalAmount?.Currency,
            CustomerName = session.CustomerName,
            CustomerEmail = session.CustomerEmail?.ToString(),
            CustomerPhone = session.CustomerPhone?.ToString(),
            CreatedAt = session.CreatedAt
        };
    }

    public static PhotoDto ToDto(this Photo photo)
    {
        return new PhotoDto
        {
            Id = photo.Id,
            SessionId = photo.SessionId,
            FilePath = photo.FilePath,
            ThumbnailPath = photo.ThumbnailPath,
            Width = photo.Dimensions.Width,
            Height = photo.Dimensions.Height,
            FileSizeBytes = photo.FileSizeBytes,
            OrderIndex = photo.OrderIndex,
            IsSelected = photo.IsSelected,
            CapturedAt = photo.CapturedAt
        };
    }

    public static PrintJobDto ToDto(this PrintJob printJob)
    {
        return new PrintJobDto
        {
            Id = printJob.Id,
            SessionId = printJob.SessionId,
            PhotoId = printJob.PhotoId,
            PrinterName = printJob.PrinterName,
            PrinterType = printJob.PrinterType.ToString(),
            CopyCount = printJob.CopyCount,
            Status = printJob.Status.ToString(),
            ErrorMessage = printJob.ErrorMessage,
            PrintedAt = printJob.PrintedAt,
            RetryCount = printJob.RetryCount
        };
    }

    public static PaymentDto ToDto(this PaymentTransaction payment)
    {
        return new PaymentDto
        {
            Id = payment.Id,
            SessionId = payment.SessionId,
            TransactionCode = payment.TransactionCode,
            Amount = payment.Amount.Amount,
            Currency = payment.Amount.Currency,
            Method = payment.Method.ToString(),
            Status = payment.Status.ToString(),
            ProviderTransactionId = payment.ProviderTransactionId,
            QrCodeData = payment.QrCodeData,
            PaidAt = payment.PaidAt,
            FailureReason = payment.FailureReason
        };
    }

    public static LayoutDto ToDto(this Layout layout)
    {
        return new LayoutDto
        {
            Id = layout.Id,
            TenantId = layout.TenantId ?? Guid.Empty,
            Name = layout.Name,
            Description = layout.Description,
            Width = layout.Dimensions.Width,
            Height = layout.Dimensions.Height,
            ConfigJson = layout.ConfigJson,
            ThumbnailPath = layout.ThumbnailPath,
            IsActive = layout.IsActive,
            DisplayOrder = layout.DisplayOrder,
            CreatedAt = layout.CreatedAt
        };
    }

    public static TemplateDto ToDto(this Template template)
    {
        return new TemplateDto
        {
            Id = template.Id,
            TenantId = template.TenantId ?? Guid.Empty,
            Name = template.Name,
            Description = template.Description,
            TemplateDataJson = template.TemplateDataJson,
            PreviewImagePath = template.PreviewImagePath,
            IsActive = template.IsActive,
            DisplayOrder = template.DisplayOrder,
            CreatedAt = template.CreatedAt
        };
    }

    public static TenantDto ToDto(this Tenant tenant)
    {
        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Code = tenant.Code,
            Email = tenant.Email?.ToString(),
            Phone = tenant.Phone?.ToString(),
            AddressStreet = tenant.Address?.Street,
            AddressCity = tenant.Address?.City,
            IsActive = tenant.IsActive,
            SubscriptionExpiryDate = tenant.SubscriptionExpiryDate,
            CreatedAt = tenant.CreatedAt
        };
    }
}
