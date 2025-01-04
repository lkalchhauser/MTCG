using MTCG.Server.Util.Enums;

namespace MTCG.Server.Models;

public class TradeOffer
{
	public int Id { get; set; }
	public int CardId { get; set; }
	public int UserId { get; set; }
	public string CardUUID { get; set; }
	public CardType? DesiredCardType { get; set; } = null;
	public Rarity? DesiredCardRarity { get; set; } = null;
	public Race? DesiredCardRace { get; set; } = null;
	public Element? DesiredCardElement { get; set; } = null;
	public float? DesiredCardMinimumDamage { get; set; } = null;
}