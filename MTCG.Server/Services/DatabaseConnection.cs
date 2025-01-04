using System.Data;
using MTCG.Server.Config;
using MTCG.Server.Models;
using MTCG.Server.Util;
using Npgsql;

namespace MTCG.Server.Services;

public class DatabaseConnection
{
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

	private static DatabaseConnection? _instance;

	public static DatabaseConnection Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new DatabaseConnection($"Host={DatabaseCredentials.DB_HOST};Port={DatabaseCredentials.DB_PORT};Username={DatabaseCredentials.DB_USER};Password={DatabaseCredentials.DB_PASSWORD};Database={DatabaseCredentials.DB_NAME};Include Error Detail=True");
			}
			return _instance;
		}
	}

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

	public static void AddParameterWithValue (IDbCommand command, string parameterName, DbType type, object? value)
	{
		IDbDataParameter parameter = command.CreateParameter();
		parameter.ParameterName = parameterName;
		parameter.DbType = type;
		parameter.Value = value ?? DBNull.Value;
		command.Parameters.Add(parameter);
	}

	//public bool RegisterUser(UserCredentials credentials)
	//{
	//	_logger.Debug("Register User - Registering user into database");
	//	var command = new NpgsqlCommand($"insert into users (username, password) values ('{credentials.Username}', '{credentials.Password}')", _connection);
	//	command.ExecuteNonQuery();
	//	return true;
	//}

	//public bool DoesUserExist(string username)
	//{
	//	_logger.Debug($"Checking if user {username} exists");
	//	var command = new NpgsqlCommand($"select * from users where username = '{username}'", _connection);
	//	var reader = command.ExecuteReader();
	//	var hasRows = reader.HasRows;
	//	reader.Close();
	//	return hasRows;
	//}

	//public string LoginUser(UserCredentials credentials)
	//{
	//	_logger.Debug("Getting password hash from database");
	//	// TODO: maybe change this to use npgsql parameters
	//	var command = new NpgsqlCommand($"select password from users where username = '{credentials.Username}'", _connection);
	//	var reader = command.ExecuteReader();

	//	if (!reader.HasRows)
	//	{
	//		// theoretically this should never happen
	//		_logger.Debug("User not found");
	//		reader.Close();
	//		return string.Empty;
	//	}

	//	reader.Read();
	//	var pwHash = (string)reader[0];
	//	reader.Close();

	//	var passwordIsValid = HelperService.VerifyPassword(credentials.Password, pwHash);

	//	if (!passwordIsValid)
	//	{
	//		_logger.Debug("Password invalid!");
	//		// TODO: maybe give a reason here instead of empty string?
	//		return string.Empty;
	//	}
	//	_logger.Debug("Password valid!");
	//	// TODO: currently we re-generate the token every login - do we want this?
	//	var userToken = HelperService.GenerateToken(credentials.Username);
	//	_logger.Debug("Saving token into database");
	//	var command2 = new NpgsqlCommand($"UPDATE users SET token = '{userToken}' WHERE username = '{credentials.Username}'", _connection);
	//	command2.ExecuteNonQuery();
	//	return userToken;

	//}
}