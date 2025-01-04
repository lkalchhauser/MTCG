namespace MTCG.Server.Models;

public class TradeAccept
{
	public int TradeId { get; set; }
	public int AcceptedUserId { get; set; }
	public int ProvidedCardId { get; set; }
}