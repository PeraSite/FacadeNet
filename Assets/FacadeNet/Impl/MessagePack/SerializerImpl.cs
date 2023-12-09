using FacadeNet.Core;
using MessagePack;

namespace FacadeNet.Impl.MessagePack {
	public class SerializerImpl : ISerializer {
		public byte[] Serialize<T>(T data) {
			return MessagePackSerializer.Serialize(data);
		}

		public T Deserialize<T>(byte[] data) {
			return MessagePackSerializer.Deserialize<T>(data);
		}
	}
}
