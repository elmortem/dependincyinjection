namespace DI
{
	// ReSharper disable once InconsistentNaming
	public interface IDIBinder
	{
		void BindInstanceAsSingle<T>(T instance) where T : class;

		void BindAsSingle<T>(params object[] parameters)
			where T : class;

		public void BindAsSingle<TImplementation, TInterface>(params object[] parameters)
			where TImplementation : class, TInterface
			where TInterface : class;

		public void BindAsSingle<TImplementation, TInterface, TInterface2>(params object[] parameters)
			where TImplementation : class, TInterface, TInterface2
			where TInterface : class
			where TInterface2 : class;

		public void BindAsSingle<TImplementation, TInterface, TInterface2, TInterface3>(params object[] parameters)
			where TImplementation : class, TInterface, TInterface2, TInterface3
			where TInterface : class
			where TInterface2 : class
			where TInterface3 : class;
	}
}