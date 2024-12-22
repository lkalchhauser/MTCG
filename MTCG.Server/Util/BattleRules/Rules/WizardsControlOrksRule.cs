using MTCG.Server.Models;
using MTCG.Server.Util.Enums;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Util.BattleRules.Rules;

public class WizardsControlOrksRule : IBattleRule
{
	public bool IsMatch(Card card1, Card card2) =>
		card1.Type == CardType.MONSTER && card2.Type == CardType.MONSTER &&
		(card1.Race == Race.WIZARD && card2.Race == Race.ORK ||
		card2.Race == Race.WIZARD && card1.Race == Race.ORK);

	public SpecialRuleResult Apply(Card card1, Card card2)
	{
		var result = new SpecialRuleResult();
		if (card1.Race == Race.WIZARD)
		{
			result.Winner = card1;
			result.LogMessage = $"{card1.Name} controls {card2.Name}!";
		}
		else
		{
			result.Winner = card2;
			result.LogMessage = $"{card2.Name} controls {card1.Name}!";
		}
		return result;
	}
}