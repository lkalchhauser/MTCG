namespace MTCG.Server.Models;

public class UserCardRelation
{
	public int UserId { get; set; }
	public int CardId { get; set; }
	public int Quantity { get; set; }
	public int LockedAmount { get; set; }
}