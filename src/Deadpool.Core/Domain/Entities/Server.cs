using Deadpool.Core.Domain.Enums;

namespace Deadpool.Core.Domain.Entities;

/// <summary>
/// Represents a SQL Server instance registered in the catalog.
/// </summary>
public sealed class Server
{
    public Guid ServerId { get; set; }
    public string ServerName { get; set; } = string.Empty;
    public ServerRole Role { get; set; }
    public string IPAddress { get; set; } = string.Empty;
    public string SqlInstance { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastHeartbeat { get; set; }
}

