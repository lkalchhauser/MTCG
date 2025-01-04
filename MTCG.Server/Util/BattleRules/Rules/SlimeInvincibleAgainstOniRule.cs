using MTCG.Server.Models;
using MTCG.Server.Util.Enums;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Util.BattleRules.Rules;

public class SlimeInvincibleAgainstOniRule : IBattleRule
{
	public bool IsMatch(Card card1, Card card2) =>
		card1.Race == Race.SLIME && card2.Race == Race.ONI ||
		card2.Race == Race.SLIME && card1.Race == Race.ONI;

	public SpecialRuleResult Apply(Card card1, Card card2)
	{
		var result = new SpecialRuleResult();
		if (card1.Race == Race.SLIME)
		{
			result.Winner = card1;
			result.LogMessage = $"{card1.Name} is immune to all of {card2.Name}s attacks!";
		}
		else
		{
			result.Winner = card2;
			result.LogMessage = $"{card2.Name} is immune to all of {card1.Name}s attacks!";
		}

		return result;
	}
}