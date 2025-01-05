using Npgsql;
using System.Data;

namespace MTCG.Server.Util;

public class DatabaseConnection : IDisposable
{
	private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

	private IDbConnection _connection;
	private string _connectionString;

	public DatabaseConnection(string connectionString)
	{
		_connectionString = connectionString;
		_connection = new NpgsqlConnection(_connectionString);
		_connection.Open();
	}

	public void Dispose()
	{
		if (_connection != null)
		{
			_connection.Close();
			_connection.Dispose();
		}
	}

	public IDbCommand CreateCommand(string commandString)
	{
		IDbCommand command = _connection.CreateCommand();
		command.CommandText = commandString;
		return command;
	}

	public static void AddParameterWithValue(IDbCommand command, string parameterName, DbType type, object? value)
	{
		IDbDataParameter parameter = command.CreateParameter();
		parameter.ParameterName = parameterName;
		parameter.DbType = type;
		parameter.Value = value ?? DBNull.Value;
		command.Parameters.Add(parameter);
	}
}