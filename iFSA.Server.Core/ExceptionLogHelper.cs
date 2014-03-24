using System;

namespace iFSA.Server
{
	public static class ExceptionLogHelper
	{
		public static void Log(this Exception e)
		{
			Console.WriteLine(e.ToString());
		}
	}
}