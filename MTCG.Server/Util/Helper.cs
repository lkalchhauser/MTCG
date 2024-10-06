namespace MTCG.Server.Util;

public class Helper
{
    public static string HashPassword(string password)
    {
        //TODO
        return "";
    }

    public static bool ValidUserCredentials(string password, string userName)
    {
        // TODO:
        return true;
    }

	 public static string GenerateToken(string username)
	{
		//TODO make this better (for test script it has to stay like this)
		return $"{username}-mtcgToken";
	}
}