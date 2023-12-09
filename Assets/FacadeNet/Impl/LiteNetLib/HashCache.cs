namespace FacadeNet.Impl.LiteNetLib {
	internal static class HashCache<T> {
		public static readonly ulong Id;

		//FNV-1 64 bit hash
		static HashCache() {
			var hash = 14695981039346656037UL; //offset
			var typeName = typeof(T).ToString();
			foreach (var ch in typeName) {
				hash ^= ch;
				hash *= 1099511628211UL; //prime
			}
			Id = hash;
		}
	}
}
