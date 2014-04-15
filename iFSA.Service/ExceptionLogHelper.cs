using System;
using System.Diagnostics;

namespace iFSA.Service
{
	public static class ExceptionLogHelper
	{
		public static void Log(this Exception e)
		{
			Trace.WriteLine(e);
		}
	}
}