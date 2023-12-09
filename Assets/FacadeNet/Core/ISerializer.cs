namespace FacadeNet.Core {
	public interface ISerializer {
		byte[] Serialize<T>(T data);
		T Deserialize<T>(byte[] data);
	}
}
