using System;
using System.Collections.Generic;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace DI
{
	// ReSharper disable once InconsistentNaming
	public static class DICache
	{
		private static readonly string _resolveMethodName = "Resolve";
		private static readonly Type _injectAttributeType = typeof(InjectAttribute);
		
		private static readonly Dictionary<Type, MethodBase> _injectMethodCache = new Dictionary<Type, MethodBase>();
		private static readonly Dictionary<Type, ConstructorInfo> _injectConstructorCache = new Dictionary<Type, ConstructorInfo>();
		private static readonly Dictionary<MethodBase, ParameterInfo[]> _methodParameterCache = new Dictionary<MethodBase, ParameterInfo[]>();
		private static readonly Dictionary<Type, MethodInfo> _resolveMethodCache = new Dictionary<Type, MethodInfo>();
		private static readonly HashSet<Type> _processedTypes = new HashSet<Type>();
		
		private static MethodInfo _resolveMethod;
		
		public static bool TryGetInjectConstructor(Type type, out ConstructorInfo method)
		{
			CacheInjectConstructorOrMethod(type);
			return _injectConstructorCache.TryGetValue(type, out method);
		}
		
		public static bool TryGetInjectMethod(Type type, out MethodBase method)
		{
			CacheInjectConstructorOrMethod(type);
			return _injectMethodCache.TryGetValue(type, out method);
		}
		
		public static ParameterInfo[] GetMethodParameters(MethodBase injectionMethod)
		{
			return _methodParameterCache[injectionMethod];
		}
		
		public static MethodInfo GetTypedResolveMethod(Type parameterType)
		{
			if (!_resolveMethodCache.TryGetValue(parameterType, out var resolveMethod))
			{
				if (_resolveMethod == null)
				{
					_resolveMethod = typeof(DIContainer).GetMethod(_resolveMethodName,
						BindingFlags.Public | BindingFlags.Instance,
						null,
						Type.EmptyTypes,
						null);
				}

				if (_resolveMethod == null)
					return null;

				resolveMethod = _resolveMethod.MakeGenericMethod(parameterType);
				_resolveMethodCache[parameterType] = resolveMethod;
			}

			return resolveMethod;
		}
		
		private static void CacheInjectConstructorOrMethod(Type type)
		{
			if (_processedTypes.Contains(type))
			{
				return;
			}

			var constructors = type.GetConstructors();
			foreach (var constructorInfo in constructors)
			{
				var hasInject = constructorInfo.IsDefined(_injectAttributeType);
				if (hasInject)
				{
					_injectConstructorCache[type] = constructorInfo;
					var methodParameters = constructorInfo.GetParameters();
					_methodParameterCache[constructorInfo] = methodParameters;
					break;
				}
			}

			var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			foreach (var methodInfo in methods)
			{
				var hasInject = methodInfo.IsDefined(_injectAttributeType);
				if (hasInject)
				{
					_injectMethodCache[type] = methodInfo;
					var methodParameters = methodInfo.GetParameters();
					_methodParameterCache[methodInfo] = methodParameters;
					break;
				}
			}

			_processedTypes.Add(type);
		}
	}
}