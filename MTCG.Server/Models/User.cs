using MTCG.Server.Util;

namespace MTCG.Server.Models;

public class User
{
    private string _username { get; set; }

    private string _passwordHash { get; set; }

    private int _coins { get; set; }

    private Stats _stats { get; set; }

    public string Password
    {
        get => _passwordHash;
        set => _passwordHash = value;
    }

    public User(string username, string password)
    {
        // TODO: validation before this can be called
        this._username = username;
        this._passwordHash = Helper.HashPassword(password);
        _stats = new Stats();
    }

    public void SetNewPassword(string newPassword)
    {
        this._passwordHash = Helper.HashPassword(newPassword);
    }

    public void ShowUser()
    {
        Console.WriteLine($"This user is {_username}");
    }
}