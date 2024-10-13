using MTCG.Server.Util.Enums;

namespace MTCG.Server.Models.Cards;

public abstract class Card
{
	private CardType _type { get; set; }

	private Element _element { get; set; }

	private Rarity _rarity { get; set; }

	private string _name { get; set; }

	private int _damage { get; set; }

	protected Card(CardType type, Element cardElement, Rarity rarity, string name, int damage)
	{
		_type = type;
		_element = cardElement;
		_rarity = rarity;
		_name = name;
		_damage = damage;
	}
}