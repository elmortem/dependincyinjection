using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace DI
{
	// ReSharper disable once InconsistentNaming
	public interface IDIBinder
	{
		void BindSingleton<T>(T instance) where T : class;
		void BindSingleton<T>(params object[] parameters) where T : class;
		void BindSingleton<TImplementation, TInterface>(TImplementation instance)
			where TImplementation : class, TInterface
			where TInterface : class;
		void BindSingleton<TImplementation, TInterface>(params object[] parameters)
			where TImplementation : class, TInterface
			where TInterface : class;
		
		void Bind<T>(T instance) where T : class;
		void Bind<T>(params object[] parameters) where T : class;
		void Bind<TImplementation, TInterface>(TImplementation instance)
			where TImplementation : class, TInterface
			where TInterface : class;
		void Bind<TImplementation, TInterface>(params object[] parameters)
			where TImplementation : class, TInterface
			where TInterface : class;
	}
}