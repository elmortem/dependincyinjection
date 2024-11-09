using System;

// ReSharper disable once CheckNamespace
namespace DI
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor)]
	public class InjectAttribute : Attribute
	{
	}
}