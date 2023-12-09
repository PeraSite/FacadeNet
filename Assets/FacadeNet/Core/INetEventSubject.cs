using System;
using FacadeNet.Core.Data;

namespace FacadeNet.Core {
	public interface INetEventSubject {
		IObservable<IConnection> ObserveConnect();
		IObservable<(IConnection connection, DisconnectInfo disconnectInfo)> ObserveDisconnect();
		IObservable<(IConnection connection, byte[] bytes)> ObserveByteReceive();
		IObservable<(IConnection connection, T packet)> ObservePacketReceive<T>();
	}
}
