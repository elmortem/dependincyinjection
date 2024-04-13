using UnityEngine;

namespace DI
{
	// ReSharper disable once InconsistentNaming
	public interface IDIInjector
	{
		void Inject(object obj);
		void Inject(GameObject obj);
	}
}