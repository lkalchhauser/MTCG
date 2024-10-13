using MTCG.Server.Util;

namespace MTCG.Server.Models;

public class User
{
    private string _username { get; set; }

    private string _password { get; set; }

    private int _coins { get; set; }

    private Stats _stats { get; set; }

	 private string _token { get; set; }

    public User(string username, string password)
    {
        _username = username;
        _password = password;
        _stats = new Stats();
        _coins = 100;
    }
}