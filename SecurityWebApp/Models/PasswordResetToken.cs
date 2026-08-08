namespace SecurityWebApp.Models;

public class PasswordResetToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string ResetToken { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
    public bool Used { get; set; }

    public User? User { get; set; }
}
