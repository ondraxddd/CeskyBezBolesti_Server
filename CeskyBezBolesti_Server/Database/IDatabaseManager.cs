using System.Data.SQLite;

namespace CeskyBezBolesti_Server.Database
{
    public interface IDatabaseManager
    {
        void RunNonQuery(string sql, Dictionary<string, object>? parameters = null);
        Task RunNonQueryAsync(string sql, Dictionary<string, object>? parameters = null);
        SQLiteDataReader RunQuery(string sql, Dictionary<string, object>? parameters = null);
        Task<SQLiteDataReader> RunQueryAsync(string sql, Dictionary<string, object>? parameters = null);

    }
}
