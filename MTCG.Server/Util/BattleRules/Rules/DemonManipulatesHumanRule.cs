using MTCG.Server.Models;
using MTCG.Server.Util.Enums;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Util.BattleRules.Rules;

public class DemonManipulatesHumanRule : IBattleRule
{
	public bool IsMatch(Card card1, Card card2) =>
		card1.Race == Race.DEMON && card2.Race == Race.HUMAN ||
		card2.Race == Race.DEMON && card1.Race == Race.HUMAN;

	public SpecialRuleResult Apply(Card card1, Card card2)
	{
		var result = new SpecialRuleResult();
		if (card1.Race == Race.DEMON)
		{
			result.Winner = card1;
			result.LogMessage = $"{card1.Name} manipulates {card2.Name} to kill themself!";
		}
		else
		{
			result.Winner = card2;
			result.LogMessage = $"{card2.Name} manipulates {card1.Name} to kill themself!";
		}

		return result;
	}
}