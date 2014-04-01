using System.IO;

namespace iFSA.Service
{
	public interface INetworkTransferable<out T>
	{
		byte[] NetworkBuffer { get; }

		T Setup(MemoryStream stream);
	}
}