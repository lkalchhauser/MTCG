namespace MTCG.Server.Util.HelperClasses;

public class Result
{
	public bool Success { get; set; }
	public string Message { get; set; }
	public string? Token;
	public string ContentType;

	// TODO: Add more properties if needed - token is theoretically not needed but would be better if tokens are replied properly
	// TODO: also maybe add a status code to the result - so we can do it like said in the yaml doc
	public Result(bool success, string message, string contentType = Helper.TEXT_PLAIN)
	{
		Success = success;
		Message = message;
		ContentType = contentType;
	}
}