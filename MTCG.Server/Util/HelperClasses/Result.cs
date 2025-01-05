using MTCG.Server.Services;

namespace MTCG.Server.Util.HelperClasses;

public class Result
{
	public bool Success { get; set; }
	public string Message { get; set; }
	public string? Token;
	public string ContentType;
	public int StatusCode { get; set; }

	// TODO: Add more properties if needed - token is theoretically not needed but would be better if tokens are replied properly
	// TODO: also maybe add a status code to the result - so we can do it like said in the yaml doc
	public Result(bool success, string message, string contentType = HelperService.TEXT_PLAIN, int statusCode = 200)
	{
		Success = success;
		Message = message;
		ContentType = contentType;
		StatusCode = statusCode;
	}
}