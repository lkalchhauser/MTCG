using MTCG.Server.Models;
using MTCG.Server.Util.Enums;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Util.BattleRules.Rules;

public class ElementEffectivenessRule : IBattleRule
{
	public bool IsMatch(Card card1, Card card2) =>
		card1.Type == CardType.SPELL || card2.Type == CardType.SPELL;

	// TODO: should the element damage only work for spells or should it apply into both directions?
	public SpecialRuleResult Apply(Card card1, Card card2)
	{
		var ruleResult = new SpecialRuleResult();
		var card1AgainstCard2Multiplier = 1.0f;
		var card2AgainstCard1Multiplier = 1.0f;

		if (card1.Type == CardType.SPELL)
		{
			card1AgainstCard2Multiplier = ElementEffectiveness.GetEffectivenessMultiplier(card1.Element, card2.Element);
			// we use else if here so the multiplier is not applied in both directions if both cards are spells
		}  else if (card2.Type == CardType.SPELL)
		{
			card2AgainstCard1Multiplier = ElementEffectiveness.GetEffectivenessMultiplier(card2.Element, card1.Element);
		}

		var card1Damage = card1.Damage * card1AgainstCard2Multiplier;
		var card2Damage = card2.Damage * card2AgainstCard1Multiplier;

		ruleResult.LogMessage = $"{card1.Name} (Dmg: {card1Damage}) vs {card2.Name} (Dmg: {card2Damage})";

		if (card1Damage > card2Damage)
		{
			ruleResult.Winner = card1;
			ruleResult.LogMessage += $" - {card1.Name} Wins!";
			return ruleResult;
		}

		if (card2Damage > card1Damage)
		{
			ruleResult.Winner = card2;
			ruleResult.LogMessage += $" - {card2.Name} Wins!";
			return ruleResult;
		}

		ruleResult.LogMessage += " - Draw!";
		return ruleResult;
	}
}