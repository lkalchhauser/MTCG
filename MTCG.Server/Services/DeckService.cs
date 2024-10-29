using System.Text.Json;
using MTCG.Server.HTTP;
using MTCG.Server.Models;
using MTCG.Server.Repositories;
using MTCG.Server.Util;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Services;

public class DeckService
{
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
	private DeckRepository _deckRepository = new DeckRepository();
	private CardRepository _cardRepository = new CardRepository();

	// currently we only allow one of each card per deck and one deck per user
	public Result GetDeckForCurrentUser(Handler handler)
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
		return new Result(true, serializedDeckCards, Helper.APPL_JSON);
	}

	public Result SetDeckForCurrentUser(Handler handler)
	{
		return new Result(true, "");
	}
}