using MTCG.Server.Models;
using MTCG.Server.Util.Enums;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Util.BattleRules.Rules;

public class KnightDrownsByWaterSpellRule : IBattleRule
{
	public bool IsMatch(Card card1, Card card2) =>
		card1.Race == Race.KNIGHT && card2 is { Type: CardType.SPELL, Element: Element.WATER } ||
		card2.Race == Race.KNIGHT && card1 is { Type: CardType.SPELL, Element: Element.WATER };

	public SpecialRuleResult Apply(Card card1, Card card2)
	{
		var result = new SpecialRuleResult();
		if (card1.Race == Race.KNIGHT)
		{
			result.Winner = card2;
			result.LogMessage = $"{card1.Name} drowns in {card2.Name}!";
		}
		else
		{
			result.Winner = card1;
			result.LogMessage = $"{card2.Name} drowns in {card1.Name}!";
		}

		return result;
	}
}