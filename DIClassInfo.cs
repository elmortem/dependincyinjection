using System;

// ReSharper disable once CheckNamespace
namespace DI
{
	// ReSharper disable once InconsistentNaming
	public class DIClassInfo
	{
		public readonly Type Type;
		public readonly Type[] InterfaceTypes;
		public readonly object[] Parameters;

		public DIClassInfo(Type type, Type[] interfaceTypes, object[] parameters)
		{
			Type = type;
			InterfaceTypes = interfaceTypes;
			Parameters = parameters;
		}
	}
}