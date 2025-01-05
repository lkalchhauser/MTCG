using MTCG.Server.Models;
using MTCG.Server.Util.BattleRules.Rules;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Util.BattleRules;

public static class BattleRules
{
	private static readonly List<IBattleRule> Rules =
	[
		new WizardsControlOrksRule(),
		new KrakenImmuneToSpellRule(),
		new GoblinFearsDragonRule(),
		new KnightDrownsByWaterSpellRule(),
		new FireElvesCanEvadeDragonRule(),
		new SlimeInvincibleAgainstOniRule(),
		new DemonManipulatesHumanRule(),
		new ElementEffectivenessRule()
	];

	public static bool Apply(Card card1, Card card2, out SpecialRuleResult winner)
	{
		foreach (var battleRule in Rules.Where(battleRule => battleRule.IsMatch(card1, card2)))
		{
			winner = battleRule.Apply(card1, card2);
			return true;
		}

		winner = new SpecialRuleResult();
		return false;
	}
}