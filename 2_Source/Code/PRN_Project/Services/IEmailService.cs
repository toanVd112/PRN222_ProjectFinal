using System.Threading.Tasks;

namespace PRN_Project.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}
