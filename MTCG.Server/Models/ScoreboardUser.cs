using MTCG.Server.Util.Enums;
using static System.Net.Mime.MediaTypeNames;
using System.Xml.Linq;
using System;

namespace MTCG.Server.Models;

public class ScoreboardUser
{
	public int Id { get; set; }
	public string Username { get; set; }
	public int Elo { get; set; }
	public int Wins { get; set; }
	public int Losses { get; set; }
	public int Draws { get; set; }
	public override string ToString()
	{
		return $"{Username}|{Elo}|{Wins}|{Losses}|{Draws}";
	}
}