using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace DI
{
	// ReSharper disable once InconsistentNaming
	public interface IDIResolver
	{
		T Resolve<T>() where T : class;
		void Inject(object obj);
		void Inject(GameObject obj);
		T Create<T>(params object[] parameters);
		object Create(Type type, params object[] parameters);
		T CreateByFactory<T>();
		object CreateByFactory(Type type);
		void Destroy(object obj);
	}
}