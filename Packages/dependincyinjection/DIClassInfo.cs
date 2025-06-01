using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace DI
{
	// ReSharper disable once InconsistentNaming
	public class DIClassInfo
	{
		public readonly Type Type;
		public readonly List<Type> InterfaceTypes = new();
		public readonly List<object> Parameters = new();

		public DIClassInfo(Type type, IEnumerable<Type> interfaceTypes, IEnumerable<object> parameters)
		{
			Type = type;
			if (interfaceTypes != null)
				InterfaceTypes.AddRange(interfaceTypes);
			if (parameters != null)
				Parameters.AddRange(parameters);
		}

		public DIClassInfo(Type type, Type[] interfaceTypes, object[] parameters) 
			: this(type, interfaceTypes, parameters as IEnumerable<object>)
		{
		}
	}
}