using System.Threading.Tasks;

namespace iFSA.Service.Core
{
	public interface ITransferHandler
	{
		Task WriteAsync(byte handlerId, byte methodId);
		Task WriteAsync(byte[] data);
		Task CloseAsync();
		Task<byte[]> ReadAsync();
	}
}