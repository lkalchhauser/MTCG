using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;

namespace MTCG.Server.HTTP;

public class Handler
{
	public virtual string PlainMessage { get; set; }

	public virtual string Method { get; set; }

	public virtual string Path { get; set; }

	public HttpHeader[] Headers { get; set; }

	public string Payload { get; set; }

	public int StatusCode { get; set; }

	public void Handle(TcpClient client)
	{
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
				Path = splitLines[1];
			} else if (inlineHeaders)
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
		}

		Console.WriteLine(data);
	}
}