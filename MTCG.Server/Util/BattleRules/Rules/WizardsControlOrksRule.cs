using MTCG.Server.Models;
using MTCG.Server.Util.Enums;

namespace MTCG.Server.Util.BattleRules.Rules;

public class WizardsControlOrksRule : IBattleRule
{
	public bool IsMatch(Card card1, Card card2) =>
		card1.Type == CardType.MONSTER && card2.Type == CardType.MONSTER &&
		(card1.Race == Race.WIZARD && card2.Race == Race.ORK ||
		card2.Race == Race.WIZARD && card1.Race == Race.ORK);

	public Card Apply(Card card1, Card card2)
	{
		return card1.Race == Race.WIZARD ? card1 : card2;
	}
}