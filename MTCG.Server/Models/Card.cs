using MTCG.Server.Util.Enums;

namespace MTCG.Server.Models;

public class Card
{
	public int Id { get; set; }
	public string UUID { get; set; }
	public CardType Type { get; set; }

	public Element Element { get; set; } = Element.NORMAL;

	public Rarity Rarity { get; set; }

	public string Name { get; set; }

	public string Description { get; set; }

	public float Damage { get; set; }

	public Race? Race { get; set; } = null;

	public override string ToString()
	{
		var s = $"UUID: {UUID}; Type: {Type}; Element: {Element}; Rarity: {Rarity}; Name: {Name}; Description '{Description}'; Damage: {Damage};";
		if (Race != null)
		{
			s += $"Race: {Race};";
		}

		return s;
	}
}