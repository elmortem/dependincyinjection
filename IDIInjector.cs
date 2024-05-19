using UnityEngine;
using UnityEngine.SceneManagement;

namespace DI
{
	// ReSharper disable once InconsistentNaming
	public interface IDIInjector
	{
		void Inject(object obj);
		void Inject(GameObject obj);
		void Inject(Scene scene);
	}
}