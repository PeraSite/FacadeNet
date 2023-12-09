using System;

namespace FacadeNet.Interface {
	public interface INetEventSubject {
		IObservable<IConnection> ObserveConnect();
		IObservable<(IConnection connection, DisconnectInfo disconnectInfo)> ObserveDisconnect();
		IObservable<(IConnection connection, byte[] bytes)> ObserveByteReceive();
		IObservable<(IConnection connection, T packet)> ObservePacketReceive<T>();
	}
}
