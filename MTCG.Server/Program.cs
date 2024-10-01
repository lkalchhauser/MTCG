namespace MTCG.Server
{
    class Program
    {
        public static void Main(string[] args)
        {
            var Server = new HTTP.Server("http://localhost:8888");
				Server.Start();
			}
    }
}