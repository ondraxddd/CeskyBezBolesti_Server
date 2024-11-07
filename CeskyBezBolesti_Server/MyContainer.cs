using CeskyBezBolesti_Server.Database;
using CeskyBezBolesti_Server.Emailing;

namespace CeskyBezBolesti_Server
{
    public static class MyContainer
    {
        private static string _conn = "Data Source=Database/database.db;";
        private static IDatabaseManager dbManager  = new SqliteDbManager(_conn);
        private static IEmailSender emailSender = new EmailSenderHostinger();
        public static IDatabaseManager GetDbManager()
        {
            return dbManager;
        }

        public static IEmailSender GetEmailSender()
        {
            return emailSender;
        }
    }
}
