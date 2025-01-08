using MTCG.Server.HTTP;
using MTCG.Server.Models;
using MTCG.Server.Repositories.Interfaces;
using MTCG.Server.Services.Interfaces;
using MTCG.Server.Util.HelperClasses;
using System.Text.Json;

namespace MTCG.Server.Services;

public class DeckService(IDeckRepository deckRepository, ICardRepository cardRepository)
	: IDeckService
{
	private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

	/**
	 *	Gets the deck for the current user (based on the handler)
	 *	This currently only works if there is only one of each card per deck and one deck per user
	 *	<param name="handler">THe handler containing the authorized user</param>
	 *	<param name="forceJsonFormat">if the output should be forced to a json format</param>
	 *	<returns>The result of the operation</returns>
	 */
	public Result GetDeckForCurrentUser(IHandler handler, bool forceJsonFormat = false)
	{
		_logger.Debug($"Getting current deck for user {handler.AuthorizedUser.Username}");
		var deckId = deckRepository.GetDeckIdFromUserId(handler.AuthorizedUser.Id);
		var cardIds = deckRepository.GetAllCardIdsFromDeckId(deckId);
		if (cardIds.Count == 0)
		{
			_logger.Debug("No cards found in deck");
			return new Result(true, "", statusCode: 204);
		}
		var deck = new Deck()
		{
			Cards = []
		};
		foreach (var cardId in cardIds)
		{
			var card = cardRepository.GetCardById(cardId);
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
			return new Result(true, finalText, HelperService.TEXT_PLAIN, statusCode: 200);
		}
		return new Result(true, serializedDeckCards, HelperService.APPL_JSON, statusCode: 200);
	}

	/**
	 *	Sets the deck for the current user
	 *	Currently only works if there is only one of each card per deck and per package
	 *	<param name="handler">The handler containing the authorized user</param>
	 *	<returns>The result of the operation</returns>
	 */
	public Result SetDeckForCurrentUser(IHandler handler)
	{
		_logger.Debug($"Setting deck for user {handler.AuthorizedUser.Username}");
		if (handler.GetContentType() != HelperService.APPL_JSON || handler.Payload == null)
		{
			_logger.Debug("SetDeckForCurrentUser - No valid payload data found");
			return new Result(false, "Badly formatted data sent!", statusCode: 400);
		}

		var cardUuids = JsonSerializer.Deserialize<List<string>>(handler.Payload);
		if (cardUuids.Count != 4)
		{
			_logger.Debug("SetDeckForCurrentUser - No valid payload data found (not enough or too many uuids)");
			return new Result(false, "Badly formatted data sent!", statusCode: 400);
		}
		List<Card> cards = [];

		foreach (var cardUuid in cardUuids)
		{
			var card = cardRepository.GetCardByUuid(cardUuid);
			if (card == null)
			{
				_logger.Debug($"No card found for card uuid \"{cardUuid}\"");
				return new Result(false, "Badly formatted data sent!", statusCode: 403);
			}
			cards.Add(card);
		}

		// TODO what if user doesnt have deck
		var currentDeckResult = GetDeckForCurrentUser(handler);
		List<Card> currentDeckCards = [];

		if (currentDeckResult.StatusCode != 204)
		{
			currentDeckCards = JsonSerializer.Deserialize<List<Card>>(currentDeckResult.Message);
		}
		else
		{
			currentDeckCards = [];
		}



		// we go through it again instead of doing it in the one above so we can have proper error logging
		foreach (var card in cards.Where(card => !IsCardAvailableForUser(card, handler.AuthorizedUser, currentDeckCards)))
		{
			_logger.Debug($"Card {card.Name} not available for user {handler.AuthorizedUser.Username}");
			return new Result(false, "Badly formatted data sent!", statusCode: 403);
		}

		var deckId = deckRepository.GetDeckIdFromUserId(handler.AuthorizedUser.Id);
		if (deckId != 0)
		{
			RemoveAndUnlockDeck(deckId, handler.AuthorizedUser, currentDeckCards);
		}

		var createdDeckId = deckRepository.AddNewDeckToUserId(handler.AuthorizedUser.Id);

		foreach (var card in cards)
		{
			var userCardRelation = cardRepository.GetUserCardRelation(handler.AuthorizedUser.Id, card.Id);
			userCardRelation.LockedAmount++;
			cardRepository.UpdateUserCardRelation(userCardRelation);
			deckRepository.AddCardToDeck(createdDeckId, card.Id);
		}

		return new Result(true, "", statusCode: 200);
	}

	/**
	 *	Removes the deck from the user and unlocks it
	 *	<param name="deckId">The deck id</param>
	 *	<param name="user">The user (owner) of the deck</param>
	 *	<param name="currentDeck">The current deck as list of cards</param>
	 */
	public void RemoveAndUnlockDeck(int deckId, UserCredentials user, List<Card> currentDeck)
	{
		foreach (var card in currentDeck)
		{
			var relation = cardRepository.GetUserCardRelation(user.Id, card.Id);
			relation.LockedAmount--;
			cardRepository.UpdateUserCardRelation(relation);
		}
		deckRepository.DeleteDeckById(deckId);
	}

	public void DeleteDeckAndCardsFromUser(int deckId, UserCredentials user, List<Card> currentDeck)
	{
		// TODO: this is not implemented yet
	}

	/**
	 * Checks if a card is available for a user
	 *	Only works if we allow one of each card per deck
	 *	<param name="card">The given card</param>
	 *	<param name="user">The user</param>
	 *	<param name="currentDeck">the current deck of the user</param>
	 *	<returns>true if the card is available, false if not</returns>
	 */
	public bool IsCardAvailableForUser(Card card, UserCredentials user, List<Card> currentDeck)
	{
		var currentAmount = 0;
		if (currentDeck.Any(currentDeckCard => currentDeckCard.Id == card.Id))
		{
			currentAmount++;
		}
		var userCardRelations = cardRepository.GetAllCardRelationsForUserId(user.Id);

		// theoretically this doesn't work if we allow the same card twice in one deck
		return userCardRelations.Where(userCardRelation => userCardRelation.CardId == card.Id).Any(userCardRelation => (userCardRelation.Quantity - userCardRelation.LockedAmount + currentAmount) > 0);
	}
}