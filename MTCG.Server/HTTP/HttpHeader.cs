namespace MTCG.Server.HTTP;

public class HttpHeader
{
	public string Name { get; private set; }

	public string Value { get; private set; }

	public HttpHeader(string name, string value)
	{
		Name = name;
		Value = value;
	}

	public HttpHeader(string header)
	{
		Name = Value = "";
		try
		{
			int n = header.IndexOf(':');
			Name = header.Substring(0, n).Trim();
			Value = header.Substring(n + 1).Trim();
		}
		catch
		{
			Console.WriteLine("Invalid header format");
		}
	}
}