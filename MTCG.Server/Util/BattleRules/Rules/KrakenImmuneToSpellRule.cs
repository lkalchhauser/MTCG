using MTCG.Server.Models;
using MTCG.Server.Util.Enums;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Util.BattleRules.Rules;

public class KrakenImmuneToSpellRule : IBattleRule
{
	public bool IsMatch(Card card1, Card card2) =>
		card1.Race == Race.KRAKEN && card2.Type == CardType.SPELL ||
		card2.Race == Race.KRAKEN && card1.Type == CardType.SPELL;

	public SpecialRuleResult Apply(Card card1, Card card2)
	{
		var result = new SpecialRuleResult();
		if (card1.Race == Race.KRAKEN)
		{
			result.Winner = card1;
			result.LogMessage = $"{card1.Name} is immune to {card2.Name}!";
		}
		else
		{
			result.Winner = card2;
			result.LogMessage = $"{card2.Name} is immune to {card1.Name}!";
		}

		return result;
	}
}