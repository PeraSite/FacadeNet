using System;
using NUnit.Framework;
using Zenject;

namespace FacadeNet.Tests.Utils {
	public abstract class DiUnitTest : IDisposable {
		protected DiContainer Container { get; private set; }

		[SetUp]
		public void Setup() {
			try {
				Container = new DiContainer();
				InstallBindings();
				Container.Inject(this);
			}
			catch (Exception e) {
				Console.WriteLine(e);
			}
		}

		protected virtual void InstallBindings() { }

		[TearDown]
		public void Teardown() {
			try {
				Dispose();
			}
			catch (Exception e) {
				Console.WriteLine(e);
			}
		}

		public virtual void Dispose() { }
	}
}
