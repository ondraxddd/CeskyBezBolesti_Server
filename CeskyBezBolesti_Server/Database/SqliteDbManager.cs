using Microsoft.VisualBasic;
using System.Data.SQLite;

namespace CeskyBezBolesti_Server.Database
{
    public class SqliteDbManager : IDatabaseManager
    {
        private string _dbConn = string.Empty;
        private SQLiteConnection _mydatabase;
        public SqliteDbManager(string conn)
        {
            _dbConn = conn;
            _mydatabase = new SQLiteConnection(conn);
            _mydatabase.Open();
        }

        public void RunNonQuery(string sql, Dictionary<string, object>? parameters = null)
        {
            using (SQLiteCommand cmd = new SQLiteCommand(sql, _mydatabase))
            {
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.Add(new SQLiteParameter(param.Key, param.Value));
                    }
                }

                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {

                }
            }
        }

        public async Task RunNonQueryAsync(string sql, Dictionary<string, object>? parameters = null)
        {
            using (SQLiteCommand cmd = new SQLiteCommand(sql, _mydatabase))
            {
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.Add(new SQLiteParameter(param.Key, param.Value));
                    }
                }

                await cmd.ExecuteNonQueryAsync();
            }
        }

        public SQLiteDataReader RunQuery(string sql, Dictionary<string, object>? parameters = null)
        {
            using (SQLiteCommand cmd = new SQLiteCommand(sql, _mydatabase))
            {

                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.Add(new SQLiteParameter(param.Key, param.Value));
                    }
                }
                SQLiteDataReader reader = cmd.ExecuteReader();


                return reader;

            }
        }
        public async Task<SQLiteDataReader> RunQueryAsync(string sql, Dictionary<string, object>? parameters = null)
        {
            throw new NotImplementedException("Není podporováno knihovnou...");
            using (SQLiteCommand cmd = new SQLiteCommand(sql, _mydatabase))
            {
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    return reader;
                }
            }
        }



    }
}
