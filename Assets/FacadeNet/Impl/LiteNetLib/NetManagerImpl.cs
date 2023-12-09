using System;
using System.Collections.Generic;
using FacadeNet.Core;
using LiteNetLib;
using LiteNetLib.Utils;
using DisconnectInfo = FacadeNet.Core.Data.DisconnectInfo;
using DisconnectReason = FacadeNet.Core.Data.DisconnectReason;

namespace FacadeNet.Impl.LiteNetLib {
	public class NetManagerImpl : INetManager {
		public INetEventSubject EventSubject => _eventSubject;
		public IReadOnlyCollection<IConnection> Connections => _connections.Values;

		private readonly ConnectionImpl.Factory _connectionFactory;
		private readonly NetEventSubjectImpl _eventSubject;

		private readonly NetManager _netManager;
		private readonly Dictionary<int, IConnection> _connections;

		public NetManagerImpl(ConnectionImpl.Factory connectionFactory, NetEventSubjectImpl eventSubject) {
			_connectionFactory = connectionFactory;
			_eventSubject = eventSubject;

			var netListener = new EventBasedNetListener();
			_netManager = new NetManager(netListener);
			_connections = new Dictionary<int, IConnection>();

			netListener.PeerConnectedEvent += peer => {
				var connection = _connectionFactory.Create(peer);
				_connections[peer.Id] = connection;
				_eventSubject.ConnectSubject.OnNext(connection);
			};

			netListener.PeerDisconnectedEvent += (peer, disconnectInfo) => {
				// 클라이언트가 서버에 접속하지 못하면 _connections에 peer가 없음
				if (!_connections.ContainsKey(peer.Id)) {
					var serverConnection = _connectionFactory.Create(peer);
					_eventSubject.DisconnectSubject.OnNext((serverConnection, ConvertLiteNetLibDisconnectInfo(disconnectInfo)));
					return;
				}

				var connection = _connections[peer.Id];
				_eventSubject.DisconnectSubject.OnNext((connection, ConvertLiteNetLibDisconnectInfo(disconnectInfo)));
			};

			netListener.NetworkReceiveEvent += (peer, reader, channel, deliveryMethod) => {
				var connection = _connections[peer.Id];

				var dataTypeByte = reader.GetByte();
				var dataType = (DataType) dataTypeByte;

				switch (dataType) {
					case DataType.RawBytes: {
						var bytes = reader.GetRemainingBytes();
						_eventSubject.BytesReceiveSubject.OnNext((connection, bytes));
						break;
					}

					case DataType.Serialized: {
						var packetType = reader.GetULong();
						var packetData = reader.GetRemainingBytes();
						_eventSubject.PacketReceiveSubject.OnNext((connection, packetType, packetData));
						break;
					}
					default:
						throw new ArgumentOutOfRangeException($"Unknown data type byte: {dataTypeByte}");
				}
			};

			netListener.ConnectionRequestEvent += request => {
				//TODO: Client should reject connection request
				request.Accept();
			};
		}

		private DisconnectInfo ConvertLiteNetLibDisconnectInfo(global::LiteNetLib.DisconnectInfo disconnectInfo) {
			var additionalDataReader = disconnectInfo.AdditionalData;

			return new DisconnectInfo {
				Reason = (DisconnectReason) disconnectInfo.Reason,
				SocketError = disconnectInfo.SocketErrorCode,
				AdditionalData = additionalDataReader.AvailableBytes > 0 ? additionalDataReader.GetRemainingBytes() : Array.Empty<byte>(),
			};
		}

		public IConnection Connect(string address, int port) {
			_netManager.Start();
			NetPeer serverPeer = _netManager.Connect(address, port, new NetDataWriter());
			var serverConnection = _connectionFactory.Create(serverPeer);
			return serverConnection;
		}

		public void Listen(int port) {
			_netManager.Start(port);
		}

		public void Stop() {
			_netManager.Stop();
		}

		public void Dispose() {
			Stop();
		}

		public void PollEvents() {
			_netManager.PollEvents();
		}
	}
}
