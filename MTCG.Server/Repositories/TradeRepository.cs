using System.Data;
using MTCG.Server.Models;
using MTCG.Server.Services;
using MTCG.Server.Util;
using MTCG.Server.Util.Enums;

namespace MTCG.Server.Repositories;

public class TradeRepository
{
	private readonly DatabaseConnection _dbConn = DatabaseConnection.Instance;
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

	public bool AddTradeOffer(TradeOffer tradeOffer)
	{
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			INSERT INTO trade_offers (offering_user_id, offered_card_id, desired_card_type, desired_card_rarity, desired_card_race, desired_card_element, desired_card_minimum_damage)
			VALUES (@offering_user_id, @offered_card_id, @desired_card_type, @desired_card_rarity, @desired_card_race, @desired_card_element, @desired_card_minimum_damage)
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@offering_user_id", DbType.Int32, tradeOffer.UserId);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@offered_card_id", DbType.Int32, tradeOffer.CardId);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@desired_card_type", DbType.String, tradeOffer.DesiredCardType.ToString());
		DatabaseConnection.AddParameterWithValue(dbCommand, "@desired_card_rarity", DbType.String, tradeOffer.DesiredCardRarity.ToString());
		DatabaseConnection.AddParameterWithValue(dbCommand, "@desired_card_race", DbType.String, tradeOffer.DesiredCardRace.ToString());
		DatabaseConnection.AddParameterWithValue(dbCommand, "@desired_card_element", DbType.String, tradeOffer.DesiredCardElement.ToString());
		DatabaseConnection.AddParameterWithValue(dbCommand, "@desired_card_minimum_damage", DbType.Int32, tradeOffer.DesiredCardMinimumDamage);
		return dbCommand.ExecuteNonQuery() == 1;
	}

	public List<TradeOffer>? GetAllTradesWithStatus(TradeStatus status)
	{
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			SELECT *
			FROM trade_offers
			WHERE status = @status
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@status", DbType.String, status.ToString());
		using IDataReader reader = dbCommand.ExecuteReader();
		List<TradeOffer> tradeOffers = [];
		while (reader.Read())
		{
			tradeOffers.Add(new TradeOffer()
			{
				Id = reader.GetInt32(0),
				UserId = reader.GetInt32(1),
				CardId = reader.GetInt32(2),
				DesiredCardType = Helper.ParseEnumOrNull<CardType>(reader.GetString(4)),
				DesiredCardRarity = Helper.ParseEnumOrNull<Rarity>(reader.GetString(4)),
				DesiredCardRace = Helper.ParseEnumOrNull<Race>(reader.GetString(5)),
				DesiredCardElement = Helper.ParseEnumOrNull<Element>(reader.GetString(6)),
				DesiredCardMinimumDamage = reader.GetInt32(7)
			});
		}
		return tradeOffers;
	}
}