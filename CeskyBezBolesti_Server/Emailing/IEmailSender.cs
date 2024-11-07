using System.Net.Mail;

namespace CeskyBezBolesti_Server.Emailing
{
    public interface IEmailSender
    {
        public void SendEmail(MailAddress destAddress, string subject, string body);
    }
}
