using System.Net;

namespace FacadeNet.Core {
	public interface IConnection {
		int Id { get; }
		IPEndPoint Endpoint { get; }

		void SendReliableOrdered(byte[] data);
		void SendReliableUnordered(byte[] data);
		void SendUnreliable(byte[] data);

		void SendReliableOrdered<T>(T data);
		void SendReliableUnordered<T>(T data);
		void SendUnreliable<T>(T data);
	}
}
