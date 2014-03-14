using System;

namespace iFSA.Service.Core
{
	public static class ExceptionLogHelper
	{
		public static void Log(this Exception e)
		{
			Console.WriteLine(e.ToString());
		}
	}
}