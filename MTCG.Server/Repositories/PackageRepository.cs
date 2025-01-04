using System.Data;
using MTCG.Server.Models;
using MTCG.Server.Repositories.Interfaces;
using MTCG.Server.Services;
using MTCG.Server.Util.Enums;

namespace MTCG.Server.Repositories;

public class PackageRepository : IPackageRepository
{
	private readonly DatabaseConnection _dbConn;
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

	public PackageRepository(DatabaseConnection dbConn)
	{
		_dbConn = dbConn;
	}

	public bool AddPackage(Package package)
	{
		_logger.Debug($"Adding package \"{package.Name}\" to the DB");
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			INSERT INTO packages (name, rarity, cost, available_amount)
			VALUES (@name, @rarity, @cost, @available_amount)
			RETURNING id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@name", DbType.String, package.Name);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@rarity", DbType.String, package.Rarity.ToString());
		DatabaseConnection.AddParameterWithValue(dbCommand, "@cost", DbType.Int32, package.Cost);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@available_amount", DbType.Int32, package.AvailableAmount);
		package.Id = (int)(dbCommand.ExecuteScalar() ?? 0);
		return package.Id != 0;
	}

	public Package? GetPackageIdByName(string name)
	{
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			SELECT *
			FROM packages
			WHERE name = @name
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@name", DbType.String, name);
		using IDataReader reader = dbCommand.ExecuteReader();
		if (reader.Read())
		{
			return new Package()
			{
				Id = reader.GetInt32(0),
				Name = reader.GetString(1),
				Rarity = Enum.Parse<Rarity>(reader.GetString(2)),
				Cost = reader.GetInt32(3),
				AvailableAmount = reader.GetInt32(4)
			};
		}

		return null;
	}

	public bool UpdatePackage(Package package)
	{
		_logger.Debug($"Updating package \"{package.Name}\"");
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			UPDATE packages
			SET name = @name, rarity = @rarity, cost = @cost, available_amount = @available_amount
			WHERE id = @id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@name", DbType.String, package.Name);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@rarity", DbType.String, package.Rarity.ToString());
		DatabaseConnection.AddParameterWithValue(dbCommand, "@cost", DbType.Int32, package.Cost);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@available_amount", DbType.Int32, package.AvailableAmount);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@id", DbType.Int32, package.Id);
		return dbCommand.ExecuteNonQuery() == 1;
	}

	public bool AddPackageCardRelation(int packageId, int cardId)
	{
		_logger.Debug($"Adding card \"{cardId}\" to package \"{packageId}\"");
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			INSERT INTO package_card (package_id, card_id)
			VALUES (@package_id, @card_id)
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@package_id", DbType.Int32, packageId);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@card_id", DbType.Int32, cardId);
		return dbCommand.ExecuteNonQuery() == 1;
	}

	public int GetRandomPackageId()
	{
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			SELECT id
			FROM packages
			ORDER BY random()
			limit 1
			""");
		using IDataReader reader = dbCommand.ExecuteReader();
		return reader.Read() ? reader.GetInt32(0) : 0;
	}

	public Package GetPackageWithoutCardsById(int id)
	{
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			SELECT *
			FROM packages
			WHERE id = @id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@id", DbType.Int32, id);
		using IDataReader reader = dbCommand.ExecuteReader();
		if (reader.Read())
		{
			return new Package()
			{
				Id = reader.GetInt32(0),
				Name = reader.GetString(1),
				Rarity = Enum.Parse<Rarity>(reader.GetString(2)),
				Cost = reader.GetInt32(3),
				AvailableAmount = reader.GetInt32(4)
			};
		}
		return null;
	}

	public List<int> GetPackageCardIds(int packageId)
	{
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			SELECT card_id
			FROM package_card
			WHERE package_id = @package_id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@package_id", DbType.Int32, packageId);
		using IDataReader reader = dbCommand.ExecuteReader();
		List<int> cardIds = [];
		while (reader.Read())
		{
			cardIds.Add(reader.GetInt32(0));
		}

		return cardIds;
	}

	public bool DeletePackage(int id)
	{
		_logger.Debug($"Deleting package with id \"{id}\"");
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			DELETE FROM packages
			WHERE id = @id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@id", DbType.Int32, id);
		return dbCommand.ExecuteNonQuery() == 1;
	}
}