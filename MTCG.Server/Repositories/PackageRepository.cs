using System.Data;
using MTCG.Server.Models;
using MTCG.Server.Services;

namespace MTCG.Server.Repositories;

public class PackageRepository
{
	private readonly DatabaseConnection _dbConn = DatabaseConnection.Instance;
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

	public bool AddPackage(Package package)
	{
		_logger.Debug($"Adding package \"{package.Name}\" to the DB");
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			INSERT INTO packages (name, rarity, cost)
			VALUES (@name, @rarity, @cost)
			RETURNING id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@name", DbType.String, package.Name);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@rarity", DbType.String, package.Rarity.ToString());
		DatabaseConnection.AddParameterWithValue(dbCommand, "@cost", DbType.Int32, package.Cost);
		package.Id = (int)(dbCommand.ExecuteScalar() ?? 0);
		return package.Id != 0;
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
}