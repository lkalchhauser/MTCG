using System.Data;
using MTCG.Server.Models;
using MTCG.Server.Services;

namespace MTCG.Server.Repositories;

public class CardRepository
{
	private readonly DatabaseConnection _dbConn = DatabaseConnection.Instance;
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

	public bool AddCard(Card card)
	{
		_logger.Debug($"Adding card \"{card.Name}\" to the DB");
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			INSERT INTO cards (uuid, name, description, damage, element, type, rarity, race)
			VALUES (@uuid, @name, @description, @damage, @element, @type, @rarity, @race)
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@uuid", DbType.String, card.UUID);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@name", DbType.String, card.Name);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@description", DbType.String, card.Description);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@damage", DbType.Int32, card.Damage);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@element", DbType.String, card.Element);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@type", DbType.String, card.Type);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@rarity", DbType.String, card.Rarity);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@race", DbType.String, card.Race);
		card.Id = (int)(dbCommand.ExecuteScalar() ?? 0);
		return card.Id != 0;
	}
}