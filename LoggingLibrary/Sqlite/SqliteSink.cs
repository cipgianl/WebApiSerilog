using Microsoft.Data.Sqlite;
using Serilog.Core;
using Serilog.Events;
using System.Data;

namespace LoggingLibrary.Sqlite
{
    public class SqliteSink : ILogEventSink
    {
        private readonly string databasePath;
        private readonly string tableName;

        public SqliteSink(string databasePath, string tableName)
        {
            this.databasePath = databasePath;
            this.tableName = tableName;

            using var connection = GetSqliteConnection();
            CreateSqlTable(connection);
        }

        public void Emit(LogEvent logEvent)
        {
            using var connection = GetSqliteConnection();

            var commandText = $"INSERT INTO {tableName} (Message, Level, Timestamp, Exception) " +
                $"VALUES (@message, @level, @timestamp, @exception)";

            using var command = new SqliteCommand(commandText, connection);
            CreateParameter(command, "@message", DbType.String, logEvent.RenderMessage());
            CreateParameter(command, "@level", DbType.String, logEvent.Level.ToString());
            CreateParameter(command, "@timestamp", DbType.DateTime, logEvent.Timestamp.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss"));
            CreateParameter(command, "@exception", DbType.String, logEvent.Exception?.ToString());

            command.ExecuteNonQuery();
        }

        private void CreateParameter(IDbCommand command, string name, DbType type, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.DbType = type;
            parameter.Value = value ?? DBNull.Value;

            command.Parameters.Add(parameter);
        }

        // Crea la connessione, la apre e la restituisce
        private SqliteConnection GetSqliteConnection()
        {
            var connectionString =
                new SqliteConnectionStringBuilder { DataSource = databasePath }.ConnectionString;
            var sqliteConnection = new SqliteConnection(connectionString);
            sqliteConnection.Open();

            return sqliteConnection;
        }

        private void CreateSqlTable(SqliteConnection sqliteConnection)
        {
            var columnDefinitions = "Id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                "Message TEXT, " +
                "Level VARCHAR(10), " +
                "Timestamp TEXT, " +
                "Exception TEXT";

            var sqlCreateText = $"CREATE TABLE IF NOT EXISTS {tableName} ({columnDefinitions})";

            using var sqlCommand = new SqliteCommand(sqlCreateText, sqliteConnection);
            sqlCommand.ExecuteNonQuery();
        }
    }
}