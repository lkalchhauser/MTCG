namespace MTCG.Server.HTTP;

/**
 *	Defines the Structure of an HTTP Header
 */
public class HttpHeader
{
	private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
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
			var n = header.IndexOf(':');
			Name = header[..n].Trim();
			Value = header[(n + 1)..].Trim();
		}
		catch
		{
			// TODO: what do?
			_logger.Error("Invalid header format!");
		}
		_logger.Debug($"Header: {Name}");
	}
}