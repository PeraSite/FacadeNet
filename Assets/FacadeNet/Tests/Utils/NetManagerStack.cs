using System;
using System.Collections.Generic;
using System.Linq;
using FacadeNet.Core;
using NUnit.Framework;

namespace FacadeNet.Tests.Utils {
	public class NetManagerStack : IDisposable {
		private readonly NetManagerFactory _factory;
		private readonly string _serverAddress;
		private readonly int _serverPort;

		private readonly Dictionary<ushort, INetManager> _serverNetManagerDict = new Dictionary<ushort, INetManager>();
		private readonly Dictionary<ushort, INetManager> _clientNetManagerDict = new Dictionary<ushort, INetManager>();

		public IEnumerable<INetManager> AllManagers => _clientNetManagerDict.Values.Concat(_serverNetManagerDict.Values);
		public IEnumerable<INetManager> Clients => _clientNetManagerDict.Values;
		public IEnumerable<INetManager> Servers => _serverNetManagerDict.Values;

		public NetManagerStack(NetManagerFactory factory, string serverAddress, int serverPort) {
			_factory = factory;
			_serverAddress = serverAddress;
			_serverPort = serverPort;
		}

		public INetManager CreateClientAndConnect(ushort id) {
			var netManager = CreateClient(id);
			netManager.Connect(_serverAddress, _serverPort);
			return netManager;
		}

		public INetManager CreateClient(ushort id) {
			AssertExistClient(id);
			var netManager = CreateNetManager();
			_clientNetManagerDict[id] = netManager;
			return netManager;
		}

		private void AssertExistClient(ushort id) {
			Assert.IsFalse(_clientNetManagerDict.ContainsKey(id), $"Already created client net manager: {id}");
		}

		public INetManager CreateServerAndListen(ushort id) {
			var netManager = CreateServer(id);
			netManager.Listen(_serverPort);
			return netManager;
		}

		public INetManager CreateServer(ushort id) {
			AssertExistServer(id);
			var netManager = CreateNetManager();
			_serverNetManagerDict[id] = netManager;
			return netManager;
		}

		private void AssertExistServer(ushort id) {
			Assert.IsFalse(_serverNetManagerDict.ContainsKey(id), $"Already created server net manager: {id}");
		}

		private INetManager CreateNetManager() {
			return _factory.Create();
		}

		public void Dispose() {
			foreach (var manager in AllManagers) {
				manager.Stop();
			}
		}
	}
}
