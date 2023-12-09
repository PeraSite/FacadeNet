using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FacadeNet.Interface;
using FacadeNet.LiteNetLib;
using FacadeNet.MessagePack;
using FacadeNet.Tests.Utils;
using LiteNetLib;
using MessagePack;
using NUnit.Framework;
using UniRx;
using Zenject;
using DisconnectInfo = FacadeNet.Interface.DisconnectInfo;
using DisconnectReason = FacadeNet.Interface.DisconnectReason;
using Random = UnityEngine.Random;

namespace FacadeNet.Tests {
	[TestFixture]
	public class LiteNetLibTest : DiUnitTest {
		private const string DEFAULT_ADDRESS = "127.0.0.1";
		private const int DEFAULT_PORT = 9050;
		private static readonly TimeSpan TimeoutTime = TimeSpan.FromSeconds(10);

		[Inject] private NetManagerStack _stack;
		private IEnumerable<INetManager> Clients => _stack.Clients;
		private IEnumerable<INetManager> Servers => _stack.Servers;

		private readonly CancellationTokenSource _cts = new CancellationTokenSource();

		protected override void InstallBindings() {
			this.Container.BindFactory<INetManager, NetManagerFactory>()
				.To<NetManagerImpl>()
				.AsSingle();

			this.Container.Bind<NetManagerStack>()
				.AsSingle()
				.WithArguments(DEFAULT_ADDRESS, DEFAULT_PORT);

			this.Container.Bind<ISerializer>()
				.To<SerializerImpl>()
				.AsSingle();

			// Concrete implementations
			this.Container.Bind<NetEventSubjectImpl>()
				.AsTransient();

			this.Container.BindFactory<NetPeer, ConnectionImpl, ConnectionImpl.Factory>()
				.AsSingle();
		}

		public override void Dispose() {
			_stack.Dispose();
		}

		[Test]
		public async Task ConnectionTest() {
			_stack.CreateServerAndListen(0);
			_stack.CreateClientAndConnect(0);

			await UntilNetworkReady();
		}

		[Test]
		[TestCase(1000)]
		[TestCase(10)]
		[TestCase(100)]
		public async Task BytesSendReceiveTest(int dataSize) {
			var server = _stack.CreateServerAndListen(0);
			var client = _stack.CreateClientAndConnect(0);
			var sendingData = CreateRandomByteArray(dataSize);
			var (source, resultTask) = CreateCompletionSource<byte[]>();

			server.EventSubject.ObserveByteReceive()
				.Subscribe(tuple => {
					(IConnection _, var receivedBytes) = tuple;
					source.TrySetResult(receivedBytes);
				});

			client.EventSubject.ObserveConnect()
				.Subscribe(peer => { peer.SendReliableOrdered(sendingData); });

			await UntilNetworkReady();

			var receivedBytes = await resultTask;
			AssertByteArraysEqual(sendingData, receivedBytes);
		}

		private void StartPollTask() => UniTask.Void(async () => {
			while (!_cts.IsCancellationRequested) {
				foreach (var client in Clients) {
					client.PollEvents();
				}
				foreach (var server in Servers) {
					server.PollEvents();
				}
				await UniTask.Delay(15, cancellationToken: _cts.Token);
			}
		});

		private static byte[] CreateRandomByteArray(int dataSize) {
			var dataArray = new byte[dataSize];
			for (var i = 0; i < dataSize; i++) {
				dataArray[i] = (byte) Random.Range(0, 255);
			}
			return dataArray;
		}

		private static TestPacket[] _testPackets = {
			new TestPacket(foo: "Foo", bar: 42, baz: new List<TestPacket.Inner> {
				new TestPacket.Inner(baz: 1),
				new TestPacket.Inner(baz: 2),
				new TestPacket.Inner(baz: 3)
			})
		};

		[Test]
		[TestCaseSource(nameof(_testPackets))]
		public async Task PacketSendReceiveTest(TestPacket packet) {
			var server = _stack.CreateServerAndListen(0);
			var client = _stack.CreateClientAndConnect(0);
			var (source, resultTask) = CreateCompletionSource<TestPacket>();

			server.EventSubject.ObservePacketReceive<TestPacket>()
				.Subscribe(tuple => {
					(IConnection _, var receivedPacket) = tuple;
					source.TrySetResult(receivedPacket);
				});

			client.EventSubject.ObserveConnect()
				.Subscribe(peer => peer.SendReliableOrdered(packet));

			await UntilNetworkReady();

			var receivedPacket = await resultTask;
			Assert.AreEqual(packet.Foo, receivedPacket.Foo);
		}

		[MessagePackObject]
		public class TestPacket {
			[Key(0)] public string Foo;
			[Key(1)] public int Bar;
			[Key(2)] public ICollection<Inner> Baz;

			public TestPacket(string foo, int bar, ICollection<Inner> baz) {
				Foo = foo;
				Bar = bar;
				Baz = baz;
			}

			[MessagePackObject]
			public class Inner {
				[Key(0)] public int Baz;

				public Inner(int baz) {
					Baz = baz;
				}
			}
		}

		[Test]
		public async Task ConnectionFailedTest() {
			INetManager client = _stack.CreateClient(0);
			var (source, resultTask) = CreateCompletionSource<(bool success, DisconnectInfo info)>();

			client.EventSubject.ObserveDisconnect()
				.Subscribe(tuple => {
					(IConnection _, var info) = tuple;
					source.TrySetResult((true, info));
				});
			client.Connect("127.0.0.2", DEFAULT_PORT);

			await UntilNetworkReady();
			var (success, disconnectInfo) = await resultTask;
			Assert.True(success);
			Assert.AreEqual(DisconnectReason.ConnectionFailed, disconnectInfo.Reason);
		}

		private async UniTask UntilNetworkReady() {
			StartPollTask();

			await UniTask.WhenAll(UntilClientConnected(), UntilServerConnected())
				.Timeout(TimeoutTime);
		}

		private async UniTask UntilClientConnected() {
			var serverCount = Servers.Count();

			while (Clients.Any(client => client.Connections.Count != serverCount)) {
				await UniTask.Delay(15, cancellationToken: _cts.Token);
			}
		}

		private async UniTask UntilServerConnected() {
			var clientCount = Clients.Count();

			while (Servers.Any(server => server.Connections.Count != clientCount)) {
				await UniTask.Delay(15, cancellationToken: _cts.Token);
			}
		}

		private static void AssertByteArraysEqual(IReadOnlyList<byte> expected, IReadOnlyList<byte> actual) {
			Assert.AreEqual(expected.Count, actual.Count);
			for (var i = 0; i < expected.Count; i++) {
				Assert.AreEqual(expected[i], actual[i]);
			}
		}

		private static (AutoResetUniTaskCompletionSource<T> source, UniTask<T> resultTask) CreateCompletionSource<T>() {
			var source = AutoResetUniTaskCompletionSource<T>.Create();
			UniTask<T> task = source.Task.Timeout(TimeoutTime);
			return (source, task);
		}
	}
}
