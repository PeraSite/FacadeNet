using System;
using System.Collections.Generic;

namespace FacadeNet.Core {
	public interface INetManager : IDisposable {
		INetEventSubject EventSubject { get; }
		IReadOnlyCollection<IConnection> Connections { get; }

		IConnection Connect(string address, int port);
		void Listen(int port);
		void Stop();
		void PollEvents();
	}
}
