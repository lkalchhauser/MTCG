using System.Text.Json;
using MTCG.Server.HTTP;
using MTCG.Server.Models;
using MTCG.Server.Repositories;
using MTCG.Server.Repositories.Interfaces;
using MTCG.Server.Util;
using MTCG.Server.Util.Enums;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Services;

public class TradeService
{
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
	private readonly ITradeRepository _tradeRepository;
	private readonly ICardRepository _cardRepository;
	private readonly IUserRepository _userRepository;
	// TODO: übergeben
	private readonly CardService _cardService;

	public TradeService(ITradeRepository tradeRepository, ICardRepository cardRepository, IUserRepository userRepository, CardService cardService)
	{
		_tradeRepository = tradeRepository;
		_cardRepository = cardRepository;
		_userRepository = userRepository;
		_cardService = cardService;
	}

	public Result CreateTradeOffer(IHandler handler)
	{
		if (handler.GetContentType() != HelperService.APPL_JSON || handler.Payload == null)
		{
			_logger.Debug("CreateTradeOffer - No valid payload data found");
			return new Result(false, "Badly formatted data sent!");
		}
		// TODO: error handling?
		var tradeOffer = JsonSerializer.Deserialize<TradeOffer>(handler.Payload);
		tradeOffer.UserId = handler.AuthorizedUser.Id;
		var card = _cardRepository.GetCardByUuid(tradeOffer.CardUUID);
		if (card == null)
		{
			_logger.Debug("CreateTradeOffer - Card not found");
			return new Result(false, "Card not found!");
		}
		tradeOffer.CardId = card.Id;

		var cardUserRelation = _cardRepository.GetUserCardRelation(handler.AuthorizedUser.Id, card.Id);
		if (cardUserRelation == null)
		{
			_logger.Debug("CreateTradeOffer - Card not owned by user");
			return new Result(false, "Card not owned by user!");
		} else if (cardUserRelation.LockedAmount == cardUserRelation.Quantity)
		{
			_logger.Debug("CreateTradeOffer - no card available to trade");
			return new Result(false, "No card available to trade!");
		}

		var tradeOfferId = _tradeRepository.AddTradeOffer(tradeOffer);
		if (!tradeOfferId)
		{
			_logger.Debug("CreateTradeOffer - Failed to add trade offer");
			return new Result(false, "Failed to add trade offer!");
		}

		_cardService.LockCardInUserStack(handler.AuthorizedUser.Id, card.Id);
		return new Result(true, "Trade deal successfully created!");
	}


	public Result GetCurrentlyActiveTrades(IHandler handler)
	{
		var currentTrades = _tradeRepository.GetAllTradesWithStatus(TradeStatus.ACTIVE);
		if (currentTrades == null)
		{
			_logger.Debug("GetCurrentlyActiveTrades - No trades found");
			return new Result(true, "No trades found!");
		}

		return !handler.HasPlainFormat() ? new Result(true, JsonSerializer.Serialize(currentTrades), HelperService.APPL_JSON) : new Result(true, GenerateTradeTable(currentTrades), HelperService.TEXT_PLAIN);
	}

	private string GenerateTradeTable(List<TradeOffer> tradeOffers)
	{
		var headers = new[] { "Id", "Card", "User", "Desired Type", " Desired Rarity", "Desired Race", "Desired Element", "Desired Minimum Damage" };

		var idWidth = Math.Max(headers[0].Length, tradeOffers.Max(e => e.Id.ToString().Length));
		//var cardIdWidth = Math.Max(headers[1].Length, tradeOffers.Max(e => e.CardId.ToString().Length));
		//var userIdWidth = Math.Max(headers[2].Length, tradeOffers.Max(e => e.UserId.ToString().Length));
		var cardIdWidth = Math.Max(headers[1].Length, tradeOffers.Max(e => GetCardNameFromId(e.CardId).Length));
		var userIdWidth = Math.Max(headers[2].Length, tradeOffers.Max(e => GetUserNameFromId(e.UserId).Length));
		var typeWidth = Math.Max(headers[3].Length, tradeOffers.Max(e => e.DesiredCardType.ToString().Length));
		var rarityWidth = Math.Max(headers[4].Length, tradeOffers.Max(e => e.DesiredCardRarity.ToString().Length));
		var raceWidth = Math.Max(headers[5].Length, tradeOffers.Max(e => e.DesiredCardRace.ToString().Length));
		var elementWidth = Math.Max(headers[6].Length, tradeOffers.Max(e => e.DesiredCardElement.ToString().Length));
		var damageWidth = Math.Max(headers[7].Length, tradeOffers.Max(e => e.DesiredCardMinimumDamage.ToString().Length));

		var headerRow = $"{headers[0].PadRight(idWidth)} | {headers[1].PadRight(cardIdWidth)} | {headers[2].PadRight(userIdWidth)} | {headers[3].PadRight(typeWidth)} | {headers[4].PadRight(rarityWidth)} | {headers[5].PadRight(raceWidth)} | {headers[6].PadRight(elementWidth)} | {headers[7].PadRight(damageWidth)}";
		var separatorRow = new string('-', headerRow.Length);

		//var rows = tradeOffers.Select(e =>
		//	$"{e.Id.ToString().PadRight(idWidth)} | {e.CardId.ToString().PadRight(cardIdWidth)} | {e.UserId.ToString().PadRight(userIdWidth)} | {e.DesiredCardType.ToString().PadRight(typeWidth)} | {e.DesiredCardRarity.ToString().PadRight(rarityWidth)} | {e.DesiredCardRace.ToString().PadRight(raceWidth)} | {e.DesiredCardElement.ToString().PadRight(elementWidth)} | {e.DesiredCardMinimumDamage.ToString().PadRight(damageWidth)}"
		//);

		var rows = tradeOffers.Select(e =>
			$"{e.Id.ToString().PadRight(idWidth)} | {GetCardNameFromId(e.CardId).PadRight(cardIdWidth)} | {GetUserNameFromId(e.UserId).PadRight(userIdWidth)} | {e.DesiredCardType.ToString().PadRight(typeWidth)} | {e.DesiredCardRarity.ToString().PadRight(rarityWidth)} | {e.DesiredCardRace.ToString().PadRight(raceWidth)} | {e.DesiredCardElement.ToString().PadRight(elementWidth)} | {e.DesiredCardMinimumDamage.ToString().PadRight(damageWidth)}"
		);

		return $"{headerRow}\n{separatorRow}\n{string.Join("\n", rows)}";
	}

	private string? GetCardNameFromId(int cardId)
	{
		var cardNameFromId = _cardRepository.GetCardById(cardId)?.Name;
		return cardNameFromId ?? null;
	}

	private string GetUserNameFromId(int userId)
	{
		var userNameFromId = _userRepository.GetUserById(userId)?.Username;
		return userNameFromId ?? "Unknown";
	}
}