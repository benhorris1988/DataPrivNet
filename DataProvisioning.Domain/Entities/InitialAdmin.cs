namespace DataProvisioning.Domain.Entities;

public class InitialAdmin
{
    public int Id { get; set; }
    
    /// <summary>
    /// The Windows domain username, e.g., "DOMAIN\username"
    /// </summary>
    public string Username { get; set; } = string.Empty;
}
