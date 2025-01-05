using MTCG.Server.Models;
using MTCG.Server.Repositories.Interfaces;
using MTCG.Server.Services.Interfaces;
using MTCG.Server.Util;
using MTCG.Server.Util.Enums;
using System.Data;
using System.Text.Json;

namespace MTCG.Server.Repositories;

/*
 *	Repository for handling all database operations related to trades.
 */
public class TradeRepository(DatabaseConnection dbConn, IHelperService helperService) : ITradeRepository
{
	private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

	/**
	 * Adds a trade offer to the database.
	 *	<param name="tradeOffer">The trade offer to add</param>
	 *	<returns>True if the trade offer was added, false otherwise</returns>
	 */
	public bool AddTradeOffer(TradeOffer tradeOffer)
	{
		_logger.Debug($"Adding trade offer: \"{JsonSerializer.Serialize(tradeOffer)}\"");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
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

	/**
	 * Gets all trades with a given status.
	 *	<param name="status">The status of the trades</param>
	 *	<returns>A list of all trades with the given status</returns>
	 */
	public List<TradeOffer>? GetAllTradesWithStatus(TradeStatus status)
	{
		_logger.Debug($"Getting all trades with status \"{status}\"");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
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
				DesiredCardType = helperService.ParseEnumOrNull<CardType>(reader.GetString(4)),
				DesiredCardRarity = helperService.ParseEnumOrNull<Rarity>(reader.GetString(4)),
				DesiredCardRace = helperService.ParseEnumOrNull<Race>(reader.GetString(5)),
				DesiredCardElement = helperService.ParseEnumOrNull<Element>(reader.GetString(6)),
				DesiredCardMinimumDamage = reader.GetInt32(7)
			});
		}
		return tradeOffers;
	}

	/**
	 * Gets all trades with a given status and a desired card type.
	 *	<param name="status">The status of the trades</param>
	 *	<param name="desiredCardType">The desired card type</param>
	 *	<returns>A list of all trades with the given status and desired card type</returns>
	 */
	public TradeOffer? GetTradeById(int tradeId)
	{
		_logger.Debug($"Getting trade with id \"{tradeId}\"");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
			SELECT *
			FROM trade_offers
			WHERE id = @trade_id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@trade_id", DbType.Int32, tradeId);
		using IDataReader reader = dbCommand.ExecuteReader();
		if (!reader.Read()) return null;

		var tradeOffer = new TradeOffer()
		{
			Id = reader.GetInt32(0),
			UserId = reader.GetInt32(1),
			CardId = reader.GetInt32(2),
			DesiredCardType = helperService.ParseEnumOrNull<CardType>(reader.GetString(3)),
			DesiredCardRarity = helperService.ParseEnumOrNull<Rarity>(reader.GetString(4)),
			DesiredCardRace = helperService.ParseEnumOrNull<Race>(reader.GetString(5)),
			DesiredCardElement = helperService.ParseEnumOrNull<Element>(reader.GetString(6)),
			DesiredCardMinimumDamage = reader.GetInt32(7),
			Status = helperService.ParseEnumOrNull<TradeStatus>(reader.GetString(9))
		};

		return tradeOffer;
	}

	/**
	 * Updates a trade offer in the database.
	 *	<param name="trade">The trade offer to update</param>
	 *	<returns>True if the trade offer was updated, false otherwise</returns>
	 */
	public bool UpdateTrade(TradeOffer trade)
	{
		_logger.Debug($"Updating trade: \"{JsonSerializer.Serialize(trade)}\"");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
			UPDATE trade_offers
			SET offering_user_id = @offering_user_id, offered_card_id = @offered_card_id, desired_card_type = @desired_card_type, desired_card_rarity = @desired_card_rarity, desired_card_race = @desired_card_race, desired_card_element = @desired_card_element, desired_card_minimum_damage = @desired_card_minimum_damage, status = @status
			WHERE offering_user_id = @offering_user_id
			AND offered_card_id = @offered_card_id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@offering_user_id", DbType.Int32, trade.UserId);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@offered_card_id", DbType.Int32, trade.CardId);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@desired_card_type", DbType.String, trade.DesiredCardType.ToString());
		DatabaseConnection.AddParameterWithValue(dbCommand, "@desired_card_rarity", DbType.String, trade.DesiredCardRarity.ToString());
		DatabaseConnection.AddParameterWithValue(dbCommand, "@desired_card_race", DbType.String, trade.DesiredCardRace.ToString());
		DatabaseConnection.AddParameterWithValue(dbCommand, "@desired_card_element", DbType.String, trade.DesiredCardElement.ToString());
		DatabaseConnection.AddParameterWithValue(dbCommand, "@desired_card_minimum_damage", DbType.Int32, trade.DesiredCardMinimumDamage);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@status", DbType.String, trade.Status.ToString());
		return dbCommand.ExecuteNonQuery() == 1;
	}

	/**
	 * Adds a trade accept entry to the database.
	 *	<param name="tradeAccept">The trade accept entry to add</param>
	 *	<returns>True if the trade accept entry was added, false otherwise</returns>
	 */
	public bool AddTradeAcceptEntry(TradeAccept tradeAccept)
	{
		_logger.Debug($"Adding trade accept entry: \"{JsonSerializer.Serialize(tradeAccept)}\"");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
			INSERT INTO trade_accept (trade_id, accepted_user_id, provided_card_id)
			VALUES (@trade_id, @accepted_user_id, @provided_card_id)
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@trade_id", DbType.Int32, tradeAccept.TradeId);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@accepted_user_id", DbType.Int32, tradeAccept.AcceptedUserId);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@provided_card_id", DbType.Int32, tradeAccept.ProvidedCardId);
		return dbCommand.ExecuteNonQuery() == 1;
	}
}