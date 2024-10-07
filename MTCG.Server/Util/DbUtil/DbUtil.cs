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
		var command2 = new NpgsqlCommand("create table users (id serial primary key, username varchar(255) not null, password varchar(255) not null, token varchar(255) not null)", connection);
		command2.ExecuteNonQuery();
	}
}