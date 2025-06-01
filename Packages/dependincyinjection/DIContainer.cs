using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
namespace DI
{
	// ReSharper disable once InconsistentNaming
	public class DIContainer : IDIContainer
	{
		private static DIContainer _root;
		public static DIContainer Root
		{
			get
			{
				if (_root == null)
				{
					_root = new DIContainer();
				}
				return _root;
			}
		}
		
		private readonly IDIContainer _parent;
		private readonly HashSet<IDisposable> _disposables = new();
		private readonly HashSet<IFactory> _factories = new();
		private readonly Dictionary<Type, IFactory> _factoriesByType = new();
		private readonly Dictionary<Type, DIClassInfo> _lazyBindClassInfos = new();
		private readonly HashSet<object> _injectedObjects = new();
		private readonly Dictionary<Type, List<object>> _typesToInstances = new();
		private readonly HashSet<Type> _singletonTypes = new();

		#region Constructor

		public DIContainer(IDIContainer parent = null)
		{
			_parent = parent;
			
			BindSingleton<IDIResolver>(this);
			BindSingleton<IDIMaker>(this);
			BindSingleton<IDIInjector>(this);
			BindSingleton<IDIContainer>(this);
			BindSingleton<IDIBinder>(this);
		}
		
		public void Dispose()
		{
			_typesToInstances.Clear();
			_injectedObjects.Clear();
			_factoriesByType.Clear();
			_factories.Clear();
			foreach (var disposable in _disposables)
			{
				disposable.Dispose();
			}
			_disposables.Clear();

			if (this == _root)
			{
				_root = null;
			}
		}
		
		#endregion

		#region Resolve

		public T Resolve<T>() where T : class
		{
			return (T)Resolve(typeof(T));
		}

		public object Resolve(Type type)
		{
			if (_typesToInstances.TryGetValue(type, out var instances) && instances.Count > 0)
			{
				return instances[0];
			}
			
			if (_lazyBindClassInfos.TryGetValue(type, out var classInfo))
			{
				var instance = InstantiateLazy(classInfo);
				_lazyBindClassInfos.Remove(type);
				if (classInfo.InterfaceTypes != null)
				{
					foreach (var interfaceType in classInfo.InterfaceTypes)
					{
						_lazyBindClassInfos.Remove(interfaceType);
					}
				}

				return instance;
			}

			if (_parent != null)
			{
				return _parent.Resolve(type);
			}

			throw new Exception($"#DI# No bind type {type}.");
		}

		public T[] ResolveAll<T>() where T : class
		{
			return ResolveAll(typeof(T)).Cast<T>().ToArray();
		}

		public object[] ResolveAll(Type type)
		{
			var result = new List<object>();
			
			if (_typesToInstances.TryGetValue(type, out var instances))
			{
				foreach (var instance in instances)
				{
					if (!_injectedObjects.Contains(instance))
					{
						Inject(instance);
						_injectedObjects.Add(instance);
					}
				}
				result.AddRange(instances);
			}
			
			if (_lazyBindClassInfos.TryGetValue(type, out var lazyClassInfo))
			{
				var instance = InstantiateLazy(lazyClassInfo);
				_lazyBindClassInfos.Remove(type);
				if (lazyClassInfo.InterfaceTypes != null)
				{
					foreach (var interfaceType in lazyClassInfo.InterfaceTypes)
					{
						_lazyBindClassInfos.Remove(interfaceType);
					}
				}
				result.Add(instance);
			}

			if (_parent != null)
			{
				result.AddRange(_parent.ResolveAll(type));
			}

			foreach (var kvp in _typesToInstances)
			{
				if (kvp.Key != type && type.IsAssignableFrom(kvp.Key))
				{
					foreach (var instance in kvp.Value)
					{
						if (!_injectedObjects.Contains(instance))
						{
							Inject(instance);
							_injectedObjects.Add(instance);
						}
						result.Add(instance);
					}
				}
			}

			return result.ToArray();
		}
		
		public bool HasType(Type type)
		{
			if (_typesToInstances.ContainsKey(type) && _typesToInstances[type].Count > 0)
			{
				return true;
			}
			
			if (_lazyBindClassInfos.ContainsKey(type))
			{
				return true;
			}

			if (_parent != null)
			{
				return ((DIContainer)_parent).HasType(type);
			}

			return false;
		}

		public bool HasType<T>() where T : class
		{
			return HasType(typeof(T));
		}

		#endregion

		#region Inject
		
		public void Inject(object obj, params object[] parameters)
		{
			if (obj == null)
			{
				Debug.LogError("Object is null");
				return;
			}
			
			if (DICache.TryGetInjectMethod(obj.GetType(), out var injectionMethod))
			{
				var paramArr = GetInjectMethodParameters(injectionMethod, parameters);
				injectionMethod.Invoke(obj, paramArr);
			}
		}

		public void Inject(GameObject obj, params object[] parameters)
		{
			var components = obj.GetComponentsInChildren<MonoBehaviour>(includeInactive:true);
			foreach (var component in components)
			{
				Inject(component, parameters);
			}
		}

		public void Inject(Scene scene, params object[] parameters)
		{
			var rootGameObjects = scene.GetRootGameObjects();
			foreach (var go in rootGameObjects)
			{
				Inject(go, parameters);
			}
		}
		
		#endregion

		#region Bind Instance

		private void BindInstanceToType(object instance, Type type, bool isSingleton = false)
		{
			if (isSingleton)
			{
				ValidateBinding(type, true);
				_singletonTypes.Add(type);
			}
			else
			{
				ValidateBinding(type, false);
			}
			
			if (!_typesToInstances.TryGetValue(type, out var instances))
			{
				instances = new List<object>();
				_typesToInstances[type] = instances;
			}
			
			if (!instances.Contains(instance))
			{
				TryRegisterDisposable(instance);
				TryRegisterFactory(instance);
				instances.Add(instance);
			}
		}
		
		#endregion // Bind Instance

		#region Singleton Binding
		
		public void BindSingleton<T>(T instance) where T : class
		{
			BindInstanceToType(instance, typeof(T), true);
		}

		public void BindSingleton<T>(params object[] parameters) where T : class
		{
			var type = typeof(T);
			if (_singletonTypes.Contains(type))
				return;
			
			LazyBind(type, null, parameters, true);
		}
		
		public void BindSingleton<TImplementation, TInterface>(TImplementation instance)
			where TImplementation : class, TInterface
			where TInterface : class
		{
			BindInstanceToType(instance, typeof(TInterface), true);
		}
		
		public void BindSingleton<TImplementation, TInterface>(params object[] parameters)
			where TImplementation : class, TInterface
			where TInterface : class
		{
			LazyBind(typeof(TImplementation), new[] { typeof(TInterface) }, parameters, true, true);
		}

		#endregion // Singleton Binding

		#region Implementation Binding

		public void Bind<T>(T instance) where T : class
		{
			BindInstanceToType(instance, typeof(T), false);
		}

		public void Bind<T>(params object[] parameters) where T : class
		{
			var type = typeof(T);
			LazyBind(type, null, parameters, false);
		}
		
		public void Bind<TImplementation, TInterface>(TImplementation instance)
			where TImplementation : class, TInterface
			where TInterface : class
		{
			BindInstanceToType(instance, typeof(TInterface), false);
		}
		
		public void Bind<TImplementation, TInterface>(params object[] parameters)
			where TImplementation : class, TInterface
			where TInterface : class
		{
			LazyBind(typeof(TImplementation), new[] { typeof(TInterface) }, parameters, false, true);
		}

		#endregion // Implementation Binding

		#region Lazy Bind

		private void ValidateBinding(Type type, bool isSingleton)
		{
			if (isSingleton && _typesToInstances.TryGetValue(type, out var instances) && instances.Count > 0)
			{
				throw new Exception($"Type {type} already has instances registered");
			}
		}

		private void LazyBind(Type type, Type[] interfaceTypes, object[] parameters, bool isSingleton = false, bool bindOnlyInterface = false)
		{
			if (isSingleton)
			{
				if (!bindOnlyInterface)
				{
					ValidateBinding(type, true);
					_singletonTypes.Add(type);
				}
				
				if (interfaceTypes != null)
				{
					foreach (var interfaceType in interfaceTypes)
					{
						ValidateBinding(interfaceType, true);
						_singletonTypes.Add(interfaceType);
					}
				}
			}
			else
			{
				if (!bindOnlyInterface)
					ValidateBinding(type, false);
					
				if (interfaceTypes != null)
				{
					foreach (var interfaceType in interfaceTypes)
					{
						ValidateBinding(interfaceType, false);
					}
				}
			}

			DIClassInfo classInfo = null;
			
			if (isSingleton)
			{
				foreach (var kvp in _lazyBindClassInfos)
				{
					if (kvp.Value.Type == type)
					{
						classInfo = kvp.Value;
						break;
					}
				}
				
				if (classInfo != null && interfaceTypes != null)
				{
					foreach (var interfaceType in interfaceTypes)
					{
						if (!classInfo.InterfaceTypes.Contains(interfaceType))
						{
							classInfo.InterfaceTypes.Add(interfaceType);
						}
					}
					
					if (parameters != null)
					{
						foreach (var parameter in parameters)
						{
							if (parameter != null && parameters.Length > 0)
							{
								classInfo.Parameters.Add(parameter);
							}
						}
					}
				}
			}
			
			if (classInfo == null)
			{
				classInfo = new DIClassInfo(type, interfaceTypes, parameters);
			}
			
			if (interfaceTypes != null)
			{
				foreach (var typeAlias in interfaceTypes)
				{
					_lazyBindClassInfos[typeAlias] = classInfo;
				}
			}

			if (!bindOnlyInterface && (interfaceTypes == null || interfaceTypes.Length <= 0))
			{
				_lazyBindClassInfos[type] = classInfo;
			}
		}

		private object InstantiateLazy(DIClassInfo classInfo)
		{
			object instance;
			if (DICache.TryGetInjectConstructor(classInfo.Type, out var constructor))
			{
				try
				{
					var parameters = GetInjectMethodParameters(constructor, classInfo.Parameters);
					instance = constructor.Invoke(parameters);
				}
				catch (Exception ex)
				{
					Debug.LogError($"[DI] Error while creating instance of type {classInfo.Type}: {ex.Message}");
					return null;
				}
			}
			else
			{
				instance = Activator.CreateInstance(classInfo.Type);
				Inject(instance);
			}

			_injectedObjects.Add(instance);
			
			var isSingleton = _singletonTypes.Contains(classInfo.Type);
			if (!isSingleton || !_typesToInstances.TryGetValue(classInfo.Type, out var instances) || instances.Count == 0)
			{
				BindInstanceToType(instance, classInfo.Type, isSingleton);
			}

			if (classInfo.InterfaceTypes != null)
			{
				foreach (var interfaceType in classInfo.InterfaceTypes)
				{
					var isInterfaceSingleton = _singletonTypes.Contains(interfaceType);
					if (!isInterfaceSingleton || !_typesToInstances.TryGetValue(interfaceType, out instances) || instances.Count == 0)
					{
						BindInstanceToType(instance, interfaceType, isInterfaceSingleton);
					}
				}
			}

			return instance;
		}

		#endregion // Lazy Bind

		public object Create(Type type, params object[] parameters)
		{
			object instance = CreateByFactoryInternal(type, parameters);
			if (instance == null)
				instance = CreateWithoutFactory(type, parameters);
			return instance;
		}
		
		public T Create<T>(params object[] parameters)
		{
			var type = typeof(T);
			return (T)Create(type, parameters);
		}
		
		public object CreateWithoutFactory(Type type, params object[] parameters)
		{
			object instance;
			if (DICache.TryGetInjectConstructor(type, out var constructor))
			{
				var finalParameters = GetInjectMethodParameters(constructor, parameters);
				instance = constructor.Invoke(finalParameters);
			}
			else
			{
				instance = Activator.CreateInstance(type);
				Inject(instance);
			}

			return instance;
		}
		
		public T CreateWithoutFactory<T>(params object[] parameters)
		{
			var type = typeof(T);
			return (T)CreateWithoutFactory(type, parameters);
		}

		public object CreateByFactory(Type type, params object[] parameters)
		{
			var result = CreateByFactoryInternal(type, parameters);
			if (result != null)
				return result;
			
			throw new Exception($"#DI# No factory for type {type}.");
		}
		
		private object CreateByFactoryInternal(Type type, params object[] parameters)
		{
			if (type == null)
			{
				Debug.LogError("Type is null!");
				return null;
			}

			if (_factoriesByType.TryGetValue(type, out var factory))
			{
				return factory.Create(type, parameters);
			}

			return null;
		}

		public T CreateByFactory<T>(params object[] parameters)
		{
			var type = typeof(T);
			return (T)CreateByFactory(type);
		}

		public void Destroy(object instance)
		{
			var type = instance.GetType();
			if (_factoriesByType.TryGetValue(type, out var factory))
			{
				factory.Destroy(instance);
			}
			else if(instance is IDisposable disposable)
			{
				disposable.Dispose();
			}
			else if (instance is GameObject gameObject)
			{
				Object.Destroy(gameObject);
			}
			else if (instance is Component component)
			{
				Object.Destroy(component);
			}
		}

		#region Utility

		private void TryRegisterDisposable(object instance)
		{
			if (instance != this && instance is IDisposable disposable && !_disposables.Contains(disposable))
			{
				_disposables.Add(disposable);
			}
		}

		private void TryRegisterFactory(object instance)
		{
			if (instance is IFactory factory)
			{
				if(_factories.Contains(factory))
					return;
				
				var genericArguments = factory.GetType().GetGenericArguments();
				if(genericArguments.Length > 0)
				{
					var baseType = genericArguments[0];
					if (_factoriesByType.TryGetValue(baseType, out var typeFactory))
					{
						throw new Exception($"#DI# Type {baseType} already has factory.");
					}

					_factories.Add(factory);
					_factoriesByType[baseType] = factory;
					
					// Sub types
					var subTypes = Assembly
						.GetAssembly(baseType)
						.GetTypes()
						.Where(t => t.IsSubclassOf(baseType));
					foreach (var type in subTypes)
					{
						_factoriesByType[type] = factory;
					}
				}
			}
		}
		
		private object[] GetInjectMethodParameters(MethodBase injectMethod, IReadOnlyList<object> parameters)
		{
			var parameterInfos = DICache.GetMethodParameters(injectMethod);

			var usedIndexes = new HashSet<int>();
			var paramArr = new object[parameterInfos.Length];
			for (var i = 0; i < parameterInfos.Length; i++)
			{
				var parameterInfo = parameterInfos[i];

				object resolved = null;

				if (parameters != null && parameters.Count > 0)
				{
					resolved = Find(parameters, (n, p) =>
					{
						if (p == null || usedIndexes.Contains(n))
							return false;
						
						var pType = p.GetType();
						if (pType == parameterInfo.ParameterType)
						{
							usedIndexes.Add(n);
							return true;
						}

						// down
						if (Array.Exists(pType.GetInterfaces(), iType => iType == parameterInfo.ParameterType))
						{
							usedIndexes.Add(n);
							return true;
						}
						
						// up
						var bType = pType.BaseType;
						while (bType != null)
						{
							if (bType == parameterInfo.ParameterType)
							{
								usedIndexes.Add(n);
								return true;
							}
							bType = bType.BaseType;
						}

						return false;
					});
				}

				if (resolved == null)
				{
					if (HasType(parameterInfo.ParameterType))
					{
						resolved = Resolve(parameterInfo.ParameterType);
					}
				}

				if (resolved == null && 
					!parameterInfo.HasDefaultValue && 
					parameterInfo.GetCustomAttribute<CanBeNullAttribute>() == null)
				{
					var genericResolveMethod = DICache.GetTypedResolveMethod(parameterInfo.ParameterType);
					if (genericResolveMethod == null)
						throw new Exception($"#DI# Generic resolve method not found for {parameterInfo.ParameterType}");
					
					resolved = genericResolveMethod.Invoke(this, null);
				}

				if (resolved == null && parameterInfo.HasDefaultValue)
				{
					resolved = parameterInfo.DefaultValue;
				}

				if (resolved == null && 
					!parameterInfo.HasDefaultValue &&
					parameterInfo.GetCustomAttribute<CanBeNullAttribute>() == null)
				{
					throw new Exception($"#DI# Parameters {parameterInfo.ParameterType} not found.");
				}

				paramArr[i] = resolved;
			}

			return paramArr;
		}

		private object Find(IReadOnlyList<object> list, Func<int, object, bool> predicate)
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (predicate(i, list[i]))
					return list[i];
			}

			return null;
		}

		#endregion // Utility
	}
}