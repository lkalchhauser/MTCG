using MTCG.Server.Models;
using MTCG.Server.Services;
using System.Net.Sockets;

namespace MTCG.Server.HTTP;

public interface IHandler
{
	public TcpClient Client { get; }
	public string PlainMessage { get; set; }
	public string Method { get; set; }
	public string Path { get; set; }
	public List<QueryParam> QueryParams { get; set; }
	public HttpHeader[] Headers { get; set; }
	public string? Payload { get; set; }
	public int StatusCode { get; set; }
	public UserCredentials AuthorizedUser { get; set; }

	public void Handle(TcpClient client);
	public void FormatPath(string path);
	public void Reply(int statusCode = 200, string? body = null, string? contentType = HelperService.TEXT_PLAIN);
	public string GetContentType();
	public string GetAuthorizationToken();
	public bool HasPlainFormat();
}