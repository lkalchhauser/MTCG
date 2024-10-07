using MTCG.Server.Config;
using MTCG.Server.Util.DbUtil;
using Npgsql;

namespace MTCG.Server
{
    class Program
    {
        public static void Main(string[] args)
        {
            //var server = new HTTP.Server("http://localhost:8888");
            //server.Start();
				var dbUtil = new DbUtil();
				dbUtil.SetupDatabase();

		}
    }
}