using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace DI
{
	// ReSharper disable once InconsistentNaming
	public interface IDIResolver
	{
		T Resolve<T>() where T : class;
		object Resolve(Type type);
		bool TryResolve<T>(out T instance) where T : class;
		bool TryResolve(Type type, out object instance);
		T[] ResolveAll<T>() where T : class;
		object[] ResolveAll(Type type);
	}
}