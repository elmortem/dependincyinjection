using UnityEngine;
using UnityEngine.SceneManagement;

namespace DI
{
	// ReSharper disable once InconsistentNaming
	public interface IDIInjector
	{
		void Inject(object obj, params object[] parameters);
		void Inject(GameObject obj, params object[] parameters);
		void Inject(Scene scene, params object[] parameters);
	}
}