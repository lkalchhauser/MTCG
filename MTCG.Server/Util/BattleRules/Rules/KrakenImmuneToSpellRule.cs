using MTCG.Server.Models;
using MTCG.Server.Util.Enums;

namespace MTCG.Server.Util.BattleRules.Rules;

public class KrakenImmuneToSpellRule : IBattleRule
{
	public bool IsMatch(Card card1, Card card2) =>
		card1.Race == Race.KRAKEN && card2.Type == CardType.SPELL ||
		card2.Race == Race.KRAKEN && card1.Type == CardType.SPELL;

	public Card Apply(Card card1, Card card2)
	{
		return card1.Race == Race.KRAKEN ? card1 : card2;
	}
}