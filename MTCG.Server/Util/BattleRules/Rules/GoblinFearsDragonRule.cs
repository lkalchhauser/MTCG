using MTCG.Server.Models;
using MTCG.Server.Util.Enums;

namespace MTCG.Server.Util.BattleRules.Rules;

public class GoblinFearsDragonRule : IBattleRule
{
	public bool IsMatch(Card card1, Card card2) =>
		card1.Type == CardType.MONSTER && card2.Type == CardType.MONSTER &&
		(card1.Race == Race.GOBLIN && card2.Race == Race.DRAGON ||
		card2.Race == Race.GOBLIN && card1.Race == Race.DRAGON);

	public Card Apply(Card card1, Card card2)
	{
		return card1.Race == Race.DRAGON ? card1 : card2;
	}
}