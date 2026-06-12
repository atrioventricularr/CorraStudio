namespace CorraStudio.Application.DTOs;

public class SessionDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string SessionCode { get; set; } = string.Empty;
    public Guid? LayoutId { get; set; }
    public string? LayoutName { get; set; }
    public Guid? TemplateId { get; set; }
    public string? TemplateName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int PhotoCount { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? Currency { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateSessionDto
{
    public Guid TenantId { get; set; }
    public string SessionCode { get; set; } = string.Empty;
    public Guid? LayoutId { get; set; }
    public Guid? TemplateId { get; set; }
}

public class UpdateSessionDto
{
    public Guid Id { get; set; }
    public Guid? LayoutId { get; set; }
    public Guid? TemplateId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
}
