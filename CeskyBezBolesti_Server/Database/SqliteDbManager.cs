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

        public void RunNonQuery(string sql)
        {
            // params (string Name, object Value)[] test - Patrik ukázka TODO
            using (SQLiteCommand cmd = new SQLiteCommand(sql, _mydatabase))
            {
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {

                }
            }
        }

        public async Task RunNonQueryAsync(string sql)
        {
            using (SQLiteCommand cmd = new SQLiteCommand(sql, _mydatabase))
            {
                await cmd.ExecuteNonQueryAsync();
            }
        }
        /*
         * SqlDataReader reader = command.ExecuteReader();

        while (reader.HasRows)
        {
            Console.WriteLine("\t{0}\t{1}", reader.GetName(0),
                reader.GetName(1));

            while (reader.Read())
            {
                Console.WriteLine("\t{0}\t{1}", reader.GetInt32(0),
                    reader.GetString(1));
            }
            reader.NextResult();
        }*/

        public SQLiteDataReader RunQuery(string sql)
        {
            using (SQLiteCommand cmd = new SQLiteCommand(sql, _mydatabase))
            {
                SQLiteDataReader reader = cmd.ExecuteReader();


                return reader;

            }
        }
        public async Task<SQLiteDataReader> RunQueryAsync(string sql)
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
