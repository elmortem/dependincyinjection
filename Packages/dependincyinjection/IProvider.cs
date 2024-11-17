namespace DI
{
	public interface IProvider
	{
	}
	
	public interface IProvider<T> : IProvider where T : class
	{
		public T Instance { get; }
	}
}