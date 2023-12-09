using System;
using FacadeNet.Core;
using FacadeNet.Core.Data;
using UniRx;

namespace FacadeNet.Impl.LiteNetLib {
	public class NetEventSubjectImpl : INetEventSubject {
		private readonly ISerializer _serializer;

		public readonly Subject<IConnection> ConnectSubject = new Subject<IConnection>();
		public readonly Subject<(IConnection, DisconnectInfo)> DisconnectSubject = new Subject<(IConnection, DisconnectInfo)>();
		public readonly Subject<(IConnection, byte[] rawBytes)> BytesReceiveSubject = new Subject<(IConnection, byte[])>();
		public readonly Subject<(IConnection, ulong packetType, byte[] packetData)> PacketReceiveSubject = new Subject<(IConnection, ulong, byte[])>();

		public NetEventSubjectImpl(ISerializer serializer) {
			_serializer = serializer;
		}

		public IObservable<IConnection> ObserveConnect() {
			return ConnectSubject;
		}

		public IObservable<(IConnection, DisconnectInfo)> ObserveDisconnect() {
			return DisconnectSubject;
		}

		public IObservable<(IConnection, byte[])> ObserveByteReceive() {
			return BytesReceiveSubject;
		}

		public IObservable<(IConnection, T)> ObservePacketReceive<T>() {
			return PacketReceiveSubject
				.Where(tuple => tuple.packetType == HashCache<T>.Id)
				.Select(tuple => (tuple.Item1, _serializer.Deserialize<T>(tuple.Item3)));
		}
	}
}
