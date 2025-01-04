using System.Text.Json;
using MTCG.Server.HTTP;
using MTCG.Server.Models;
using MTCG.Server.Repositories;
using MTCG.Server.Repositories.Interfaces;
using MTCG.Server.Util;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Services;

public class DeckService
{
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
	private readonly IDeckRepository _deckRepository;
	private readonly ICardRepository _cardRepository;

	public DeckService(IDeckRepository deckRepository, ICardRepository cardRepository)
	{
		_deckRepository = deckRepository;
		_cardRepository = cardRepository;
	}

	// currently we only allow one of each card per deck and one deck per user
	public Result GetDeckForCurrentUser(IHandler handler, bool forceJsonFormat = false)
	{
		_logger.Debug($"Getting current deck for user {handler.AuthorizedUser.Username}");
		var deckId = _deckRepository.GetDeckIdFromUserId(handler.AuthorizedUser.Id);
		var cardIds = _deckRepository.GetAllCardIdsFromDeckId(deckId);
		var deck = new Deck()
		{
			Cards = []
		};
		foreach (var cardId in cardIds)
		{
			var card = _cardRepository.GetCardById(cardId);
			if (card == null)
			{
				_logger.Debug($"No card found for card id \"{cardId}\"");
				continue;
			}
			deck.Cards.Add(card);
		}

		var serializedDeckCards = JsonSerializer.Serialize(deck.Cards);
		_logger.Debug($"Current deck for user {handler.AuthorizedUser.Username} is: {serializedDeckCards}");

		if (handler.HasPlainFormat() && !forceJsonFormat)
		{
			var finalText = deck.Cards.Aggregate("", (current, deckCard) => current + (deckCard + "\n"));
			return new Result(true, finalText, HelperService.TEXT_PLAIN);
		}
		return new Result(true, serializedDeckCards, HelperService.APPL_JSON);
	}

	// all of this currently only works if there is only one of each card per package
	public Result SetDeckForCurrentUser(IHandler handler)
	{
		_logger.Debug($"Setting deck for user {handler.AuthorizedUser.Username}");
		if (handler.GetContentType() != HelperService.APPL_JSON || handler.Payload == null)
		{
			_logger.Debug("SetDeckForCurrentUser - No valid payload data found");
			return new Result(false, "Badly formatted data sent!");
		}

		var cardUuids = JsonSerializer.Deserialize<List<string>>(handler.Payload);
		if (cardUuids.Count != 4)
		{
			_logger.Debug("SetDeckForCurrentUser - No valid payload data found (not enough or too many uuids)");
			return new Result(false, "Badly formatted data sent!");
		}
		List<Card> cards = [];

		foreach (var cardUuid in cardUuids)
		{
			var card = _cardRepository.GetCardByUuid(cardUuid);
			if (card == null)
			{
				_logger.Debug($"No card found for card uuid \"{cardUuid}\"");
				return new Result(false, "Badly formatted data sent!");
			}
			cards.Add(card);
		}

		
		var currentDeckResult = GetDeckForCurrentUser(handler);
		var currentDeckCards = JsonSerializer.Deserialize<List<Card>>(currentDeckResult.Message);

		// we go through it again instead of doing it in the one above so we can have proper error logging
		foreach (var card in cards.Where(card => !IsCardAvailableForUser(card, handler.AuthorizedUser, currentDeckCards)))
		{
			_logger.Debug($"Card {card.Name} not available for user {handler.AuthorizedUser.Username}");
			return new Result(false, "Badly formatted data sent!");
		}

		var deckId = _deckRepository.GetDeckIdFromUserId(handler.AuthorizedUser.Id);
		if (deckId != 0)
		{
			RemoveAndUnlockDeck(deckId, handler.AuthorizedUser, currentDeckCards);
		}

		var createdDeckId = _deckRepository.AddNewDeckToUserId(handler.AuthorizedUser.Id);

		foreach (var card in cards)
		{
			var userCardRelation = _cardRepository.GetUserCardRelation(handler.AuthorizedUser.Id, card.Id);
			userCardRelation.LockedAmount++;
			_cardRepository.UpdateUserStack(userCardRelation);
			_deckRepository.AddCardToDeck(createdDeckId, card.Id);
		}

		return new Result(true, "");
	}

	public void RemoveAndUnlockDeck(int deckId, UserCredentials user, List<Card> currentDeck)
	{
		foreach (var card in currentDeck)
		{
			var relation = _cardRepository.GetUserCardRelation(user.Id, card.Id);
			relation.LockedAmount--;
			_cardRepository.UpdateUserStack(relation);
		}
		_deckRepository.DeleteDeckById(deckId);
	}

	public void DeleteDeckAndCardsFromUser(int deckId, UserCredentials user, List<Card> currentDeck)
	{
		// TODO: this is not implemented yet
	}

	public bool IsCardAvailableForUser(Card card, UserCredentials user, List<Card> currentDeck)
	{
		var currentAmount = 0;
		if (currentDeck.Any(currentDeckCard => currentDeckCard.Id == card.Id))
		{
			currentAmount++;
		}
		var userCardRelations = _cardRepository.GetAllCardRelationsForUserId(user.Id);

		// theoretically this doesn't work if we allow the same card twice in one deck
		return userCardRelations.Where(userCardRelation => userCardRelation.CardId == card.Id).Any(userCardRelation => (userCardRelation.Quantity - userCardRelation.LockedAmount + currentAmount) > 0);
	}
}