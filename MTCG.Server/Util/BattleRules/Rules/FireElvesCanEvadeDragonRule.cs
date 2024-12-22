using MTCG.Server.Models;
using MTCG.Server.Util.Enums;

namespace MTCG.Server.Util.BattleRules.Rules;

public class FireElvesCanEvadeDragonRule : IBattleRule
{
	public bool IsMatch(Card card1, Card card2) =>
		card1.Type == CardType.MONSTER && card2.Type == CardType.MONSTER &&
		(card1.Race == Race.FIRE_ELVES && card2.Race == Race.DRAGON ||
		card2.Race == Race.FIRE_ELVES && card1.Race == Race.DRAGON);

	public Card Apply(Card card1, Card card2)
	{
		return card1.Race == Race.FIRE_ELVES ? card1 : card2;
	}
}