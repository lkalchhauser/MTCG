using MTCG.Server.Models;
using MTCG.Server.Util.Enums;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Util.BattleRules.Rules;

public class GoblinFearsDragonRule : IBattleRule
{
	public bool IsMatch(Card card1, Card card2) =>
		card1.Type == CardType.MONSTER && card2.Type == CardType.MONSTER &&
		(card1.Race == Race.GOBLIN && card2.Race == Race.DRAGON ||
		card2.Race == Race.GOBLIN && card1.Race == Race.DRAGON);

	public SpecialRuleResult Apply(Card card1, Card card2)
	{
		var result = new SpecialRuleResult();
		if (card1.Race == Race.DRAGON)
		{
			result.Winner = card1;
			result.LogMessage = $"{card1.Name} scares {card2.Name} away!";
		}
		else
		{
			result.Winner = card2;
			result.LogMessage = $"{card2.Name} scares {card1.Name} away!";
		}
		return result;
	}
}