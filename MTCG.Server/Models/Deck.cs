using MTCG.Server.Models.Cards;

namespace MTCG.Server.Models;

public class Deck
{
	private User _owner;

	private List<Card> _cards;
}