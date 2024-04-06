using System;

// ReSharper disable once CheckNamespace
namespace DI
{
	public interface IFactory
	{
		object Create(Type type);
		void Destroy(object instance);
	}

	public interface IFactory<in TBase> : IFactory
	{
		T Create<T>() where T : TBase;
	}
}