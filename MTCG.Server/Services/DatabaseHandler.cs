using MTCG.Server.Config;
using MTCG.Server.Models;
using MTCG.Server.Util;
using Npgsql;

namespace MTCG.Server.Services;

public class DatabaseHandler
{
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
	private static DatabaseHandler? _instance = null;

	private static readonly object _lock = new object();

	private NpgsqlConnection _connection;

	public static DatabaseHandler Instance
	{
		get
		{
			lock (_lock)
			{
				_instance ??= new DatabaseHandler();

				return _instance;
			}

		}
	}

	public bool SetupDbConnection()
	{
		try
		{
			_connection = new NpgsqlConnection(new DatabaseCredentials().GetConnectionString());
			_connection.Open();
			_logger.Info("Database connection established!");
			return true;
		}
		catch (Exception e)
		{
			_logger.Error(e, "Couldn't open database connection!");
			return false;
		}
	}

	public bool CloseDbConnection()
	{
		_connection.Close();
		_logger.Info("Database connection closed!");
		return true;
	}

	public bool RegisterUser(UserCredentials credentials)
	{
		_logger.Debug("Register User - Registering user into database");
		var command = new NpgsqlCommand($"insert into users (username, password) values ('{credentials.Username}', '{credentials.Password}')", _connection);
		command.ExecuteNonQuery();
		return true;
	}

	public bool DoesUserExist(string username)
	{
		_logger.Debug($"Checking if user {username} exists");
		var command = new NpgsqlCommand($"select * from users where username = '{username}'", _connection);
		var reader = command.ExecuteReader();
		var hasRows = reader.HasRows;
		reader.Close();
		return hasRows;
	}

	public string LoginUser(UserCredentials credentials)
	{
		_logger.Debug("Getting password hash from database");
		// TODO: maybe change this to use npgsql parameters
		var command = new NpgsqlCommand($"select password from users where username = '{credentials.Username}'", _connection);
		var reader = command.ExecuteReader();

		if (!reader.HasRows)
		{
			// theoretically this should never happen
			_logger.Debug("User not found");
			reader.Close();
			return string.Empty;
		}

		reader.Read();
		var pwHash = (string)reader[0];
		reader.Close();

		var passwordIsValid = Helper.VerifyPassword(credentials.Password, pwHash);

		if (!passwordIsValid)
		{
			_logger.Debug("Password invalid!");
			// TODO: maybe give a reason here instead of empty string?
			return string.Empty;
		}
		_logger.Debug("Password valid!");
		// TODO: currently we re-generate the token every login - do we want this?
		var userToken = Helper.GenerateToken(credentials.Username);
		_logger.Debug("Saving token into database");
		var command2 = new NpgsqlCommand($"UPDATE users SET token = '{userToken}' WHERE username = '{credentials.Username}'", _connection);
		command2.ExecuteNonQuery();
		return userToken;

	}
}