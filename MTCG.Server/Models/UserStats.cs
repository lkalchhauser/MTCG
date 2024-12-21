namespace MTCG.Server.Models;

public class UserStats
{
	public int Id { get; set; }
	public int Elo { get; set; }
	public int Wins { get; set; }
	public int Losses { get; set; }
	public int Draws { get; set; }
}