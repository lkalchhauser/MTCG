using MTCG.Server.Util.Enums;

namespace MTCG.Server.Models;

// TODO: currently this is very similar to Cards, maybe abstract class?
public class UserCards
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
	public int Quantity { get; set; }
	public int LockedAmount { get; set; }
}