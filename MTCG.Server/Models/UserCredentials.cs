namespace MTCG.Server.Models;

public class UserCredentials
{
	public int Id { get; set; }
	public string Username { get; set; }
	public string Password { get; set; }
	public string? Token { get; set; }
}