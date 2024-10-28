using System.Data;
using System.Text.Json;
using MTCG.Server.Models;
using MTCG.Server.Services;

namespace MTCG.Server.Repositories;

public class UserRepository
{
	private readonly DatabaseConnection _dbConn = DatabaseConnection.Instance;
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

	public UserCredentials? GetUserByUsername(string username)
	{
		_logger.Debug($"Trying to get user \"{username}\" from db");
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			SELECT *
			FROM users
			WHERE username = @username
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@username", DbType.String, username);

		using IDataReader reader = dbCommand.ExecuteReader();
		if (reader.Read())
		{
			return new UserCredentials()
			{
				Id = reader.GetInt32(0),
				Username = reader.GetString(1),
				Password = reader.GetString(2),
				Token = reader[3] as string ?? null,
				Coins = reader.GetInt32(4)
			};
		}
		return null;
	}

	public UserCredentials? GetUserByToken(string token)
	{
		_logger.Debug($"Trying to get user from db");
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
		                                                   SELECT *
		                                                   FROM users
		                                                   WHERE token = @token
		                                                   """);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@token", DbType.String, token);

		using IDataReader reader = dbCommand.ExecuteReader();
		if (reader.Read())
		{
			return new UserCredentials()
			{
				Id = reader.GetInt32(0),
				Username = reader.GetString(1),
				Password = reader.GetString(2),
				Token = reader[3] as string ?? null,
				Coins = reader.GetInt32(4)
			};
		}
		return null;
	}

	public bool AddUser(UserCredentials user)
	{
		_logger.Debug($"Adding user \"{JsonSerializer.Serialize(user)}\" to db");
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			INSERT INTO users (username, password)
			VALUES (@username, @password)
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@username", DbType.String, user.Username);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@password", DbType.String, user.Password);
		return dbCommand.ExecuteNonQuery() == 1;
	}

	public bool UpdateUser(UserCredentials user)
	{
		_logger.Debug($"Updating user \"{JsonSerializer.Serialize(user)}\" in db");
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			UPDATE users
			SET username = @username, password = @password, token = @token, coins = @coins
			WHERE id = @id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@username", DbType.String, user.Username);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@password", DbType.String, user.Password);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@token", DbType.String, user.Token);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@id", DbType.Int32, user.Id);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@coins", DbType.Int32, user.Coins);
		return dbCommand.ExecuteNonQuery() == 1;
	}

	public bool RemoveUser(int userId)
	{
		_logger.Debug($"Removing user with id {userId} from db");
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			DELETE FROM users
			WHERE id = @id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@id", DbType.Int32, userId);
		return dbCommand.ExecuteNonQuery() == 1;
	}
}