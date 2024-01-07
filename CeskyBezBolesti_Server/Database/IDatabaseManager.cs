using System.Data.SQLite;

namespace CeskyBezBolesti_Server.Database
{
    public interface IDatabaseManager
    {
        void RunNonQuery(string sql);
        Task RunNonQueryAsync(string sql);
        SQLiteDataReader RunQuery(string sql);
        Task<SQLiteDataReader> RunQueryAsync(string sql);

    }
}
