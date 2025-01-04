using System.Data;
using MTCG.Server.Models;
using MTCG.Server.Repositories.Interfaces;
using MTCG.Server.Services;
using MTCG.Server.Util;
using MTCG.Server.Util.Enums;

namespace MTCG.Server.Repositories;

public class TradeRepository : ITradeRepository
{
	private readonly DatabaseConnection _dbConn;
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
	private readonly IHelperService _helperService;

	public TradeRepository(DatabaseConnection dbConn, IHelperService helperService)
	{
		_dbConn = dbConn;
		_helperService = helperService;
	}

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
				DesiredCardType = _helperService.ParseEnumOrNull<CardType>(reader.GetString(4)),
				DesiredCardRarity = _helperService.ParseEnumOrNull<Rarity>(reader.GetString(4)),
				DesiredCardRace = _helperService.ParseEnumOrNull<Race>(reader.GetString(5)),
				DesiredCardElement = _helperService.ParseEnumOrNull<Element>(reader.GetString(6)),
				DesiredCardMinimumDamage = reader.GetInt32(7)
			});
		}
		return tradeOffers;
	}
}