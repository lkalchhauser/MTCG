using MTCG.Server.Util.Enums;

namespace MTCG.Server.Models;

public class Package
{
	public int Id { get; set; }
	public string Name { get; set; }

	public int Cost { get; set; } = 5;
    
	public Rarity Rarity { get; set; }

	public List<Card> Cards { get; set; }

	public int AvailableAmount { get; set; } = 1;
}