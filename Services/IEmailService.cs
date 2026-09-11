namespace SecureSistem.Services
{
    /// <summary>
    /// Sends transactional emails (password resets, notifications, etc).
    /// </summary>
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string body);
    }
}
