using System.Diagnostics;
using XOAuth.Domain;

namespace XOAuth.Base
{
	public class Logger
	{
		public void Log(object message, LogLevel level = LogLevel.Debug, string module = "XOAuth")
		{
			Debug.WriteLine($"[{level}] {module}: {message.ToString()}");
		}
	}
}
