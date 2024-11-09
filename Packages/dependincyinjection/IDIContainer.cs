using System;

namespace DI
{
	// ReSharper disable once InconsistentNaming
	public interface IDIContainer : IDIBinder, IDIMaker, IDIResolver, IDIInjector, IDisposable
	{
	}
}