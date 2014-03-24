using System;

namespace iFSA.Service
{
	public static class ExceptionLogHelper
	{
		public static void Log(this Exception e)
		{
			Console.WriteLine(e.ToString());
		}
	}
}