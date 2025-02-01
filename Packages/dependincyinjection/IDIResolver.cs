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
		T[] ResolveAll<T>() where T : class;
		object[] ResolveAll(Type type);
	}
}