using MTCG.Server.Models;
using MTCG.Server.Util.Enums;

namespace MTCG.Server.Util.BattleRules.Rules;

public class KnightDrownsByWaterSpellRule : IBattleRule
{
	public bool IsMatch(Card card1, Card card2) =>
		card1.Race == Race.KNIGHT && card2 is { Type: CardType.SPELL, Element: Element.WATER } ||
		card2.Race == Race.KNIGHT && card1 is { Type: CardType.SPELL, Element: Element.WATER };

	public Card Apply(Card card1, Card card2)
	{
		return card1.Race == Race.KNIGHT ? card2 : card1;
	}
}