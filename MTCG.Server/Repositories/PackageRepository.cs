using MTCG.Server.Models;
using MTCG.Server.Repositories.Interfaces;
using MTCG.Server.Util;
using MTCG.Server.Util.Enums;
using System.Data;

namespace MTCG.Server.Repositories;

/*
 *	Repository for handling all database operations related to packages.
 */
public class PackageRepository(DatabaseConnection dbConn) : IPackageRepository
{
	private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

	/**
	 * Adds a package to the database.
	 *	<param name="package">The package to add</param>
	 *	<returns>True if the package was added, false otherwise</returns>
	 */
	public bool AddPackage(Package package)
	{
		_logger.Debug($"Adding package \"{package.Name}\" to the DB");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
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

	/**
	 * Gets a package by its id.
	 *	<param name="id">The id of the package</param>
	 *	<returns>The package with the given id</returns>
	 */
	public Package? GetPackageIdByName(string name)
	{
		_logger.Debug($"Getting package with name \"{name}\" from the DB");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
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

	/**
	 *	Update a package in the database.
	 *	<param name="package">The package to update</param>
	 *	<returns>True if the package was successfully updated, otherwise false</returns>
	 */
	public bool UpdatePackage(Package package)
	{
		_logger.Debug($"Updating package \"{package.Name}\"");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
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

	/**
	 *	Adds a card to a package (relation).
	 *	<param name="packageId">The id of the package</param>
	 *	<param name="cardId">The id of the card</param>
	 *	<returns>True if the card was added to the package, otherwise false</returns>
	 */
	public bool AddPackageCardRelation(int packageId, int cardId)
	{
		_logger.Debug($"Adding card \"{cardId}\" to package \"{packageId}\"");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
			INSERT INTO package_card (package_id, card_id)
			VALUES (@package_id, @card_id)
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@package_id", DbType.Int32, packageId);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@card_id", DbType.Int32, cardId);
		return dbCommand.ExecuteNonQuery() == 1;
	}

	/**
	 * Gets a random package id.
	 *	<returns>A random package id</returns>
	 */
	public int GetRandomPackageId()
	{
		_logger.Debug("Getting random package id");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
			SELECT id
			FROM packages
			ORDER BY random()
			limit 1
			""");
		using IDataReader reader = dbCommand.ExecuteReader();
		return reader.Read() ? reader.GetInt32(0) : 0;
	}

	/**
	 * Gets a package by its id without the cards.
	 *	<param name="id">The id of the package</param>
	 *	<returns>The package with the given id (without any card infos)</returns>
	 */
	public Package GetPackageWithoutCardsById(int id)
	{
		_logger.Debug($"Getting package with id \"{id}\"");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
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

	/**
	 * Gets all card ids from a given package id.
	 *	<param name="packageId">The package id</param>
	 *	<returns>A list of all card ids from the given package id</returns>
	 */
	public List<int> GetPackageCardIds(int packageId)
	{
		_logger.Debug($"Getting card ids from package \"{packageId}\"");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
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

	/**
	 * Deletes a package by its id.
	 *	<param name="id">The id of the package</param>
	 *	<returns>True if the package was deleted, false otherwise</returns>
	 */
	public bool DeletePackage(int id)
	{
		_logger.Debug($"Deleting package with id \"{id}\"");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
			DELETE FROM packages
			WHERE id = @id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@id", DbType.Int32, id);
		return dbCommand.ExecuteNonQuery() == 1;
	}
}