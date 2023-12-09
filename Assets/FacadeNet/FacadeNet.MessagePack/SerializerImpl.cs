using FacadeNet.Interface;
using MessagePack;

namespace FacadeNet.MessagePack {
	public class SerializerImpl : ISerializer {
		public byte[] Serialize<T>(T data) {
			return MessagePackSerializer.Serialize(data);
		}

		public T Deserialize<T>(byte[] data) {
			return MessagePackSerializer.Deserialize<T>(data);
		}
	}
}
