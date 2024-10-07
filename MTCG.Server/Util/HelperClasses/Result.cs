namespace MTCG.Server.Util.HelperClasses;

public class Result
{
	public bool Success { get; set; }
	public string Message { get; set; }
	public string? Token;

	// TODO: Add more properties if needed - token is theoretically not needed but would be better if tokens are replied properly
	public Result(bool success, string message)
	{
		Success = success;
		Message = message;
	}
}