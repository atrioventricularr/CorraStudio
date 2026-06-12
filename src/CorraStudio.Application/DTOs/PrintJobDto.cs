namespace CorraStudio.Application.DTOs;

public class PrintJobDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid PhotoId { get; set; }
    public string PrinterName { get; set; } = string.Empty;
    public string PrinterType { get; set; } = string.Empty;
    public int CopyCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime? PrintedAt { get; set; }
    public int RetryCount { get; set; }
}

public class CreatePrintJobDto
{
    public Guid SessionId { get; set; }
    public Guid PhotoId { get; set; }
    public string PrinterName { get; set; } = string.Empty;
    public string PrinterType { get; set; } = string.Empty;
    public int CopyCount { get; set; } = 1;
}
