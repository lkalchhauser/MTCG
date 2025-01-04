using System.Text.Json;
using MTCG.Server.HTTP;
using MTCG.Server.Models;
using MTCG.Server.Repositories;
using MTCG.Server.Repositories.Interfaces;
using MTCG.Server.Services.Interfaces;
using MTCG.Server.Util;
using MTCG.Server.Util.Enums;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Services;

public class TradeService(
	ITradeRepository tradeRepository,
	ICardRepository cardRepository,
	IUserRepository userRepository,
	ICardService cardService,
	IDeckService deckService)
	: ITradeService
{
	private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

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
		var card = cardRepository.GetCardByUuid(tradeOffer.CardUUID);
		if (card == null)
		{
			_logger.Debug("CreateTradeOffer - Card not found");
			return new Result(false, "Card not found!");
		}
		tradeOffer.CardId = card.Id;

		var cardUserRelation = cardRepository.GetUserCardRelation(handler.AuthorizedUser.Id, card.Id);
		if (cardUserRelation == null)
		{
			_logger.Debug("CreateTradeOffer - Card not owned by user");
			return new Result(false, "Card not owned by user!");
		} else if (cardUserRelation.LockedAmount == cardUserRelation.Quantity)
		{
			_logger.Debug("CreateTradeOffer - no card available to trade");
			return new Result(false, "No card available to trade!");
		}

		var tradeOfferId = tradeRepository.AddTradeOffer(tradeOffer);
		if (!tradeOfferId)
		{
			_logger.Debug("CreateTradeOffer - Failed to add trade offer");
			return new Result(false, "Failed to add trade offer!");
		}

		cardService.LockCardInUserStack(handler.AuthorizedUser.Id, card.Id);
		return new Result(true, "Trade deal successfully created!");
	}


	public Result GetCurrentlyActiveTrades(IHandler handler)
	{
		var currentTrades = tradeRepository.GetAllTradesWithStatus(TradeStatus.ACTIVE);
		if (currentTrades == null)
		{
			_logger.Debug("GetCurrentlyActiveTrades - No trades found");
			return new Result(true, "No trades found!");
		}

		return !handler.HasPlainFormat() ? new Result(true, JsonSerializer.Serialize(currentTrades), HelperService.APPL_JSON) : new Result(true, GenerateTradeTable(currentTrades), HelperService.TEXT_PLAIN);
	}

	public Result DeleteTrade(IHandler handler)
	{
		int.TryParse(handler.Path.Split("/").Last(), out var tradingId);
		var trade = tradeRepository.GetTradeById(tradingId);

		if (trade == null)
		{
			_logger.Debug("DeleteTrade - Trade not found");
			return new Result(false, "Trade not found!");
		}

		if (!trade.UserId.Equals(handler.AuthorizedUser.Id))
		{
			_logger.Debug("DeleteTrade - User is not owner of the trade!");
			return new Result(false, "User is not owner of the trade!");
		}

		if (trade.Status != TradeStatus.ACTIVE)
		{
			_logger.Debug("DeleteTrade - Trade is not active");
			return new Result(false, "Trade is not active!");
		}

		trade.Status = TradeStatus.DELETED;
		var tradeDeleted = tradeRepository.UpdateTrade(trade);
		if (!tradeDeleted)
		{
			_logger.Debug("DeleteTrade - Failed to delete trade");
			return new Result(false, "Failed to delete trade!");
		}

		return new Result(true, "Trade successfully deleted!");
	}

	public Result AcceptTradeOffer(IHandler handler)
	{
		if (handler.GetContentType() != HelperService.APPL_JSON || handler.Payload == null)
		{
			_logger.Debug("AcceptTradeOffer - No valid payload data found");
			return new Result(false, "Badly formatted data sent!");
		}

		var tradeAccept = JsonSerializer.Deserialize<TradeAcceptRequest>(handler.Payload);

		int.TryParse(handler.Path.Split("/").Last(), out var tradingId);
		var trade = tradeRepository.GetTradeById(tradingId);

		if (trade == null)
		{
			_logger.Debug("DeleteTrade - Trade not found");
			return new Result(false, "Trade not found!");
		}

		if (trade.UserId.Equals(handler.AuthorizedUser.Id))
		{
			_logger.Debug("AcceptTrade - user tried to trade with themselves!");
			return new Result(false, "You cannot trade with yourself!");
		}

		if (trade.Status != TradeStatus.ACTIVE)
		{
			_logger.Debug("DeleteTrade - Trade is not active");
			return new Result(false, "Trade is not active!");
		}

		

		
		var acceptCard = cardRepository.GetCardByUuid(tradeAccept?.UUID);

		if (acceptCard == null)
		{
			_logger.Debug("AcceptTrade - Card not found");
			return new Result(false, "Card not found!");
		}

		if (!cardService.IsCardAvailableForUser(acceptCard.Id, handler.AuthorizedUser.Id))
		{
			_logger.Debug("AcceptTrade - Card is not available!");
			return new Result(false, "Card is not available!");
		}

		var isValidTradeResult = IsCardValidToTrade(trade, acceptCard);

		if (!isValidTradeResult.Success)
		{
			return new Result(false, isValidTradeResult.Message);
		}

		var offerCard = cardRepository.GetCardById(trade.CardId);

		cardService.RemoveCardFromUserStack(handler.AuthorizedUser.Id, acceptCard.Id);
		cardService.UnlockCardInUserStack(trade.UserId, trade.CardId);
		cardService.RemoveCardFromUserStack(trade.UserId, trade.CardId);
		cardService.AddCardToUserStack(handler.AuthorizedUser.Id, offerCard.Id);
		cardService.AddCardToUserStack(trade.UserId, acceptCard.Id);

		trade.Status = TradeStatus.ACCEPTED;
		var tradeUpdated = tradeRepository.UpdateTrade(trade);

		var tradeAcceptObject = new TradeAccept()
		{
			AcceptedUserId = handler.AuthorizedUser.Id,
			TradeId = trade.Id,
			ProvidedCardId = acceptCard.Id
		};

		var tradeAcceptLog = tradeRepository.AddTradeAcceptEntry(handler, tradeAcceptObject);

		if (!tradeUpdated)
		{
			_logger.Debug("AcceptTrade - Failed to update trade");
			return new Result(false, "Failed to update trade!");
		}

		if (!tradeAcceptLog)
		{
			_logger.Debug("AcceptTrade - Failed to log trade accept");
			return new Result(false, "Failed to log trade accept!");
		}

		return new Result(true, "Trade successfully accepted!");
	}

	private Result IsCardValidToTrade(TradeOffer offer, Card acceptCard)
	{
		if (offer.DesiredCardType != null)
		{
			if (offer.DesiredCardType != acceptCard.Type)
			{
				return new Result(false, "Card type does not match!");
			}
		}

		if (offer.DesiredCardRarity != null)
		{
			// TODO: maybe accept any card higher than the rarity?
			if (offer.DesiredCardRarity != acceptCard.Rarity)
			{
				return new Result(false, "Card rarity does not match!");
			}
		}

		if (offer.DesiredCardRace != null)
		{
			if (offer.DesiredCardRace != acceptCard.Race)
			{
				return new Result(false, "Card race does not match!");
			}
		}

		if (offer.DesiredCardElement != null)
		{
			if (offer.DesiredCardElement != acceptCard.Element)
			{
				return new Result(false, "Card element does not match!");
			}
		}

		if (offer.DesiredCardMinimumDamage != null)
		{
			if (offer.DesiredCardMinimumDamage > acceptCard.Damage)
			{
				return new Result(false, "Card damage is too low!");
			}
		}

		return new Result(true, "Trade is valid!");
	}

	private string GenerateTradeTable(List<TradeOffer> tradeOffers)
	{
		if (tradeOffers.Count == 0)
		{
			return "No trades found!";
		}

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
		var cardNameFromId = cardRepository.GetCardById(cardId)?.Name;
		return cardNameFromId ?? null;
	}

	private string GetUserNameFromId(int userId)
	{
		var userNameFromId = userRepository.GetUserById(userId)?.Username;
		return userNameFromId ?? "Unknown";
	}
}