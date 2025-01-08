using MTCG.Server.Services;

namespace MTCG.Server.Util.HelperClasses;

public class Result
{
	public bool Success { get; set; }
	public string Message { get; set; }
	public string? Token;
	public string ContentType;
	public int StatusCode { get; set; }

	public Result(bool success, string message, string contentType = HelperService.TEXT_PLAIN, int statusCode = 200)
	{
		Success = success;
		Message = message;
		ContentType = contentType;
		StatusCode = statusCode;
	}
}