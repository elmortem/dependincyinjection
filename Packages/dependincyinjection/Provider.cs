namespace DI
{
	public class Provider<T> : IProvider<T> where T : class
	{
		private IDIResolver _resolver;
		private T _instance;

		[Inject]
		public Provider(IDIResolver resolver)
		{
			_resolver = resolver;
		}
		
		public T Instance 
		{
			get
			{
				if (_instance == null)
				{
					_instance = _resolver.Resolve<T>();
				}

				return _instance;
			}
		}
	}
}