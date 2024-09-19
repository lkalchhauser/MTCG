using MTCG.Server.Util.Enums;

namespace MTCG.Server.Models;

public class SpellCard : Card
{
	public SpellCard(CardType type, Element cardElement, Rarity rarity, string name, int damage) : base(type, cardElement, rarity, name, damage)
	{
	}
}