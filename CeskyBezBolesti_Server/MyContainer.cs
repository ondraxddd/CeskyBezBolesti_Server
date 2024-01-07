using CeskyBezBolesti_Server.Database;

namespace CeskyBezBolesti_Server
{
    public static class MyContainer
    {
        private static string _conn = "Data Source=Database/database.db;";
        private static IDatabaseManager dbManager  = new SqliteDbManager(_conn);
        public static IDatabaseManager GetDbManager()
        {
            return dbManager;
        }
    }
}
