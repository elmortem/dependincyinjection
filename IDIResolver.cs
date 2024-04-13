using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace DI
{
	// ReSharper disable once InconsistentNaming
	public interface IDIResolver
	{
		T Resolve<T>() where T : class;
	}
}