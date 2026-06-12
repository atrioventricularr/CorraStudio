namespace CorraStudio.Application.DTOs;

public class TenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? AddressStreet { get; set; }
    public string? AddressCity { get; set; }
    public bool IsActive { get; set; }
    public DateTime? SubscriptionExpiryDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTenantDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? AddressStreet { get; set; }
    public string? AddressCity { get; set; }
}

public class UpdateTenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? AddressStreet { get; set; }
    public string? AddressCity { get; set; }
    public bool IsActive { get; set; }
    public DateTime? SubscriptionExpiryDate { get; set; }
}
