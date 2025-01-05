using MTCG.Server.Models;
using MTCG.Server.Services;
using MTCG.Server.Services.Interfaces;
using System.Net.Sockets;
using System.Text;

namespace MTCG.Server.HTTP;

public class Handler : IHandler
{
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
	public TcpClient Client { get; private set; }
	public virtual string PlainMessage { get; set; }

	public virtual string Method { get; set; }

	public virtual string Path { get; set; }
	public List<QueryParam> QueryParams { get; set; } = [];

	public HttpHeader[] Headers { get; set; }

	public string? Payload { get; set; }

	public int StatusCode { get; set; }

	public UserCredentials AuthorizedUser { get; set; }
	public readonly IHelperService _helperService;

	public Handler(IHelperService helperService)
	{
		_helperService = helperService;
	}

	/**
	 *	Handles the request from the TcpClient
	 *	This parses the request and adds any needed properties to the Handler class
	 */
	public void Handle(TcpClient client)
	{
		_logger.Debug("Handling request");
		Client = client;
		var buffer = new byte[1024];
		var data = "";
		var stream = client.GetStream();
		while (stream.DataAvailable || data == "")
		{
			var n = stream.Read(buffer, 0, buffer.Length);
			data += Encoding.UTF8.GetString(buffer, 0, n);
		}

		PlainMessage = data;

		var requestLines = data.Replace("\r\n", "\n").Replace("\r", "\n").Split("\n");
		var inlineHeaders = true;
		var headers = new List<HttpHeader>();

		for (var i = 0; i < requestLines.Length; i++)
		{
			if (i == 0)
			{
				var splitLines = requestLines[0].Split(" ");
				Method = splitLines[0];
				FormatQueryParams(splitLines[1]);
			}
			else if (inlineHeaders)
			{
				if (string.IsNullOrWhiteSpace(requestLines[i]))
				{
					inlineHeaders = false;
				}
				else
				{
					headers.Add(new HttpHeader(requestLines[i]));
				}
			}
			else
			{
				Payload += requestLines[i] + "\r\n";
			}

			Headers = headers.ToArray();

			//_logger.Debug($"Headers: {JsonSerializer.Serialize(Headers)}, Path: {JsonSerializer.Serialize(Path)}, Payload: {JsonSerializer.Serialize(Payload)}");
		}
	}

	/**
	 * Gets all the query params from the path and adds them to the QueryParams list
	 *	<param name="path">The path to format</param>>
	 */
	public void FormatQueryParams(string path)
	{
		if (path.Contains('?'))
		{
			var split1Path = path.Split("?");
			Path = split1Path[0];

			if (split1Path[1].Contains('&'))
			{
				var split2Path = split1Path[1].Split("&");
				foreach (var split in split2Path)
				{
					var split3Path = split.Split("=");
					QueryParams.Add(new QueryParam()
					{
						Key = split3Path[0],
						Value = split3Path[1]
					});
				}

				return;
			};

			var splitQueryPath = split1Path[1].Split("=");
			QueryParams.Add(new QueryParam()
			{
				Key = splitQueryPath[0],
				Value = splitQueryPath[1]
			});
		}
		else
		{
			Path = path;
		}
	}

	/**
	 * Replies to the request with the given status code, body and content type
	 *	<param name="statusCode">The status code to reply with</param>
	 *	<param name="body">The body to reply with</param>
	 *	<param name="contentType">The content type to reply with</param>
	 */
	public async void Reply(int statusCode = 200, string? body = null, string? contentType = HelperService.TEXT_PLAIN)
	{
		_logger.Debug("Replying to request");
		StatusCode = statusCode;
		var response = $"HTTP/1.1 {statusCode} {_helperService.GetHttpCodes()[statusCode]}\n";
		if (string.IsNullOrEmpty(body))
		{
			response += "Content-Length: 0\n";
		}

		response += $"Content-Type: {contentType}\n\n";

		if (body != null)
		{
			response += body;
		}

		_logger.Debug($"Sending response: {response}");

		var tmpBuf = Encoding.ASCII.GetBytes(response);
		await Client.GetStream().WriteAsync(tmpBuf, 0, tmpBuf.Length);
		Client.GetStream().Close();
		Client.Dispose();
	}

	/**
	 * Gets the content type from the headers
	 */
	public string GetContentType()
	{
		foreach (var httpHeader in Headers)
		{
			if (httpHeader.Name == "Content-Type")
			{
				return httpHeader.Value;
			}
		}
		return "";
	}

	/**
	 * Gets the authorization token from the headers
	 */
	public string GetAuthorizationToken()
	{
		foreach (var httpHeader in Headers)
		{
			if (httpHeader.Name == "Authorization")
			{
				return httpHeader.Value.Replace("Bearer ", "");
			}
		}
		return "";
	}

	/**
	 * Checks if the query params contain a format of plain
	 */
	public bool HasPlainFormat()
	{
		return QueryParams.Any(param => param is { Key: "format", Value: "plain" });
	}
}