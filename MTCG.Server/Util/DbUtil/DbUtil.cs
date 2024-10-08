using MTCG.Server.Config;
using Npgsql;

namespace MTCG.Server.Util.DbUtil;

public class DbUtil
{
	public void SetupDatabase()
	{
		var connection = new NpgsqlConnection(new DatabaseCredentials().GetConnectionString());
		connection.Open();
		var command = new NpgsqlCommand("drop table if exists users cascade", connection);
		command.ExecuteNonQuery();
		var command2 = new NpgsqlCommand("create table users (id serial primary key, username varchar not null, password varchar not null, token varchar)", connection);
		command2.ExecuteNonQuery();
		connection.Close();
	}
}