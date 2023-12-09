using System.Net;
using FacadeNet.Core;
using LiteNetLib;
using LiteNetLib.Utils;
using Zenject;

namespace FacadeNet.Impl.LiteNetLib {
	public class ConnectionImpl : IConnection {
		public int Id => _peer.Id;
		public IPEndPoint Endpoint => _peer.EndPoint;

		private readonly NetPeer _peer;
		private readonly ISerializer _serializer;

		private readonly NetDataWriter _writer = new NetDataWriter();

		public ConnectionImpl(NetPeer peer, ISerializer serializer) {
			_peer = peer;
			_serializer = serializer;
		}

		public void SendReliableOrdered(byte[] data) {
			SendBytes(data, DeliveryMethod.ReliableOrdered);
		}

		public void SendReliableUnordered(byte[] data) {
			SendBytes(data, DeliveryMethod.ReliableUnordered);
		}

		public void SendUnreliable(byte[] data) {
			SendBytes(data, DeliveryMethod.Unreliable);
		}

		private void SendBytes(byte[] data, DeliveryMethod deliveryMethod) {
			_writer.Reset();
			_writer.Put((byte) DataType.RawBytes);
			_writer.Put(data);

			_peer.Send(_writer, deliveryMethod);
		}

		public void SendReliableOrdered<T>(T data) {
			SendSerializable(data, DeliveryMethod.ReliableOrdered);
		}

		public void SendReliableUnordered<T>(T data) {
			SendSerializable(data, DeliveryMethod.ReliableUnordered);
		}

		public void SendUnreliable<T>(T data) {
			SendSerializable(data, DeliveryMethod.Unreliable);
		}

		private void SendSerializable<T>(T data, DeliveryMethod deliveryMethod) {
			var serialized = _serializer.Serialize(data);

			_writer.Reset();
			_writer.Put((byte) DataType.Serialized);
			_writer.Put(HashCache<T>.Id);
			_writer.Put(serialized);

			_peer.Send(_writer, deliveryMethod);
		}

		protected bool Equals(ConnectionImpl other) {
			return Equals(_peer.Id, other._peer.Id);
		}

		public override bool Equals(object obj) {
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((ConnectionImpl) obj);
		}

		public override int GetHashCode() {
			return (_peer != null ? _peer.Id.GetHashCode() : 0);
		}

		public static bool operator ==(ConnectionImpl left, ConnectionImpl right) {
			return Equals(left, right);
		}

		public static bool operator !=(ConnectionImpl left, ConnectionImpl right) {
			return !Equals(left, right);
		}

		public class Factory : PlaceholderFactory<NetPeer, ConnectionImpl> { }
	}
}
