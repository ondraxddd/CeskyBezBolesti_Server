using System.Net;
using System.Net.Mail;

namespace CeskyBezBolesti_Server.Emailing
{
    public class EmailSenderHostinger : IEmailSender
    {
        public void SendEmail(MailAddress destAddress, string subject, string body)
        {

            MailAddress to = destAddress;
            MailAddress from = new MailAddress("podpora@ceskybezbolesti.com", "CeskyBezBolesti");

            MailMessage email = new MailMessage(from, to);
            email.Subject = subject;
            email.Body = body;

            SmtpClient smtp = new SmtpClient();
            smtp.Host = "smtp.hostinger.com";
            smtp.Port = 587;
            smtp.Credentials = new NetworkCredential("podpora@ceskybezbolesti.com", "oR9#woZ]1");
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtp.EnableSsl = false;

            try
            {
               smtp.Send(email);
            }
            catch (SmtpException ex)
            {
               // Console.WriteLine(ex.ToString());
            }
        }
    }
}
