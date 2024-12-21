namespace MTCG.Server.Models;

public class Scoreboard
{
	public DateTime LastUpdated { get; set; }
	public List<ScoreboardUser> ScoreboardUsers;
}