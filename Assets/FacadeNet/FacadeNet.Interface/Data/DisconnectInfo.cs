using System.Net.Sockets;

namespace FacadeNet.Interface {
	public enum DisconnectReason {
		ConnectionFailed,
		Timeout,
		HostUnreachable,
		NetworkUnreachable,
		RemoteConnectionClose,
		DisconnectPeerCalled,
		ConnectionRejected,
		InvalidProtocol,
		UnknownHost,
		Reconnect,
		PeerToPeerConnection,
		PeerNotFound,
	}

	public struct DisconnectInfo {
		public DisconnectReason Reason;
		public SocketError SocketError;
		public byte[] AdditionalData;
	}
}
