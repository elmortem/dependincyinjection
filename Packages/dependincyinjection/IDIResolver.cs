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
		/// <summary>
		/// Проверяет, зарегистрирован ли тип в контейнере, без создания его экземпляра
		/// </summary>
		bool HasType<T>() where T : class;
		/// <summary>
		/// Проверяет, зарегистрирован ли тип в контейнере, без создания его экземпляра
		/// </summary>
		bool HasType(Type type);
	}
}