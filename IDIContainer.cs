using System;

namespace DI
{
	// ReSharper disable once InconsistentNaming
	public interface IDIContainer : IDIInjector, IDIResolver
	{
		T Create<T>(params object[] parameters);
		object Create(Type type, params object[] parameters);
		T CreateWithoutFactory<T>(params object[] parameters);
		object CreateWithoutFactory(Type type, params object[] parameters);
		T CreateByFactory<T>(params object[] parameters);
		object CreateByFactory(Type type, params object[] parameters);
		void Destroy(object obj);
	}
}