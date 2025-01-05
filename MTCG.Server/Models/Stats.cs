namespace MTCG.Server.Models;

public class Stats
{
	private int _losses { get; set; } = 0;

	private int _wins { get; set; } = 0;

	private int _draws { get; set; } = 0;

	private int _elo { get; set; } = 100;
}