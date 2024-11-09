using System;

// ReSharper disable once CheckNamespace
namespace DI
{
	public interface IFactory
	{
		object Create(Type type, params object[] parameters);
		void Destroy(object instance);
	}

	public interface IFactory<in TBase> : IFactory
	{
		T Create<T>(params object[] parameters) where T : TBase;
	}
}