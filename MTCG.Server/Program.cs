using MTCG.Server.Models;
using MTCG.Server.Util.DbUtil;
using System.Text.Json;
using MTCG.Server.Repositories;

namespace MTCG.Server
{
	internal class Program
	{
		private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

		public static void Main(string[] args)
		{
			var dbUtil = new DbUtil();
			dbUtil.SetupDatabase();
			var server = new HTTP.Server("http://localhost:10001");
			server.Start();
		}
	}
}