using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
namespace DI
{
	// ReSharper disable once InconsistentNaming
	public class DIContainer : IDIResolver, IDisposable
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
		
		private readonly DIContainer _parent;
		private readonly Dictionary<Type, object> _typesToInstances = new();
		
		private readonly Dictionary<Type, DIClassInfo> _lazyBindClassInfos = new();
		
		private readonly List<object> _injectedObjects = new();
		private readonly List<IDisposable> _disposables = new();
		private readonly Dictionary<Type, IFactory> _factories = new();

		private bool _completed;

		#region Constructor

		public DIContainer(DIContainer parent = null)
		{
			_parent = parent;
			
			BindInstanceAsSingle<IDIResolver>(this);
		}
		
		public void Dispose()
		{
			_typesToInstances.Clear();
			_injectedObjects.Clear();
			foreach (var disposable in _disposables)
			{
				disposable.Dispose();
			}
			_disposables.Clear();
		}
		
		#endregion

		#region Resolve

		public T Resolve<T>() where T : class
		{
			var type = typeof(T);
			
			if (!_completed)
			{
				_completed = true;
				
				var uniqueInstances = new HashSet<object>(_typesToInstances.Values);
				foreach (var instanceToInject in uniqueInstances)
				{
					Inject(instanceToInject);
				}
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

				return (T)instance;
			}

			if (!_typesToInstances.TryGetValue(type, out var value))
			{
				if (_parent != null)
				{
					return _parent.Resolve<T>();
				}

				throw new Exception($"#DI# No bind type {type}.");
			}

			return (T)value;
		}
		
		#endregion

		#region Inject
		
		public void Inject(object obj)
		{
			if (_injectedObjects.Contains(obj))
			{
				return;
			}

			if (DICache.TryGetInjectMethod(obj.GetType(), out var injectionMethod))
			{
				var paramArr = GetInjectMethodParameters(injectionMethod, null);
				injectionMethod.Invoke(obj, paramArr);
				_injectedObjects.Add(obj);
			}
		}

		public void Inject(GameObject obj)
		{
			var components = obj.GetComponentsInChildren<Component>(includeInactive:true);
			foreach (var component in components)
			{
				Inject(component);
			}
		}

		public void Inject(Scene scene)
		{
			var rootGameObjects = scene.GetRootGameObjects();
			foreach (var go in rootGameObjects)
			{
				Inject(go);
			}
		}
		
		#endregion

		#region Bind Instance

		public void BindInstanceAsSingle<T>(T instance) where T : class
		{
			BindInstanceToType(instance, typeof(T));
		}

		/*public void BindInstanceAsSingle<TImplementation, TInterface>(TImplementation instance)
			where TImplementation : class, TInterface
			where TInterface : class
		{
			BindInstanceAsSingle<TInterface>(instance);
			//BindInstanceAsSingle(instance);
		}

		public void BindInstanceAsSingle<TImplementation, TInterface, TInterface2>(TImplementation instance)
			where TImplementation : class, TInterface, TInterface2
			where TInterface : class
			where TInterface2 : class
		{
			BindInstanceAsSingle<TInterface>(instance);
			BindInstanceAsSingle<TInterface2>(instance);
			//BindInstanceAsSingle(instance);
		}
		
		public void BindInstanceAsSingle<TImplementation, TInterface, TInterface2, TInterface3>(TImplementation instance)
			where TImplementation : class, TInterface, TInterface2, TInterface3
			where TInterface : class
			where TInterface2 : class
			where TInterface3 : class
		{
			BindInstanceAsSingle<TInterface>(instance);
			BindInstanceAsSingle<TInterface2>(instance);
			BindInstanceAsSingle<TInterface3>(instance);
		}*/

		private void BindInstanceToType(object instance, Type type)
		{
			_typesToInstances[type] = instance;
		}
		
		#endregion // Bind Instance

		#region Lazy Bind

		public void BindAsSingle<T>(params object[] parameters)
			where T : class
		{
			LazyBind(typeof(T), null, parameters);
		}
		
		public void BindAsSingle<TImplementation, TInterface>(params object[] parameters)
			where TImplementation : class, TInterface
			where TInterface : class
		{
			LazyBind(typeof(TImplementation), new []{typeof(TInterface)}, parameters);
		}

		public void BindAsSingle<TImplementation, TInterface, TInterface2>(params object[] parameters)
			where TImplementation : class, TInterface, TInterface2
			where TInterface : class
			where TInterface2 : class
		{
			LazyBind(typeof(TImplementation), new []{typeof(TInterface), typeof(TInterface2)}, parameters);
		}

		public void BindAsSingle<TImplementation, TInterface, TInterface2, TInterface3>(params object[] parameters)
			where TImplementation : class, TInterface, TInterface2, TInterface3
			where TInterface : class
			where TInterface2 : class
			where TInterface3 : class
		{
			LazyBind(typeof(TImplementation), new []{typeof(TInterface), typeof(TInterface2)}, parameters);
		}

		private void LazyBind(Type type, Type[] interfaceTypes, object[] parameters)
		{
			if (_completed)
			{
				Debug.LogError("Container is completed!");
				return;
			}
			
			var classInfo = new DIClassInfo(type, interfaceTypes, parameters);
			if (interfaceTypes != null)
			{
				foreach (var typeAlias in interfaceTypes)
				{
					_lazyBindClassInfos[typeAlias] = classInfo;
				}
			}

			if (interfaceTypes == null || interfaceTypes.Length <= 0)
			{
				_lazyBindClassInfos[type] = classInfo;
			}
		}

		private object InstantiateLazy(DIClassInfo classInfo)
		{
			object instance;
			if (DICache.TryGetInjectConstructor(classInfo.Type, out var constructor))
			{
				var parameters = GetInjectMethodParameters(constructor, classInfo.Parameters);
				instance = constructor.Invoke(parameters);
			}
			else
			{
				instance = Activator.CreateInstance(classInfo.Type);
				Inject(instance);
			}

			TryRegisterDisposable(instance);
			TryRegisterFactory(instance);

			BindInstanceToType(instance, classInfo.Type);

			if (classInfo.InterfaceTypes != null)
			{
				foreach (var interfaceType in classInfo.InterfaceTypes)
				{
					BindInstanceToType(instance, interfaceType);
				}
			}

			return instance;
		}

		#endregion // Lazy Bind

		public object Create(Type type, params object[] parameters)
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
		
		public T Create<T>(params object[] parameters)
		{
			var type = typeof(T);
			return (T)Create(type, parameters);
		}

		public object CreateByFactory(Type type)
		{
			if (_factories.TryGetValue(type, out var factory))
			{
				return factory.Create(type);
			}

			throw new Exception($"#DI# No factory for type {type}.");
		}

		public T CreateByFactory<T>()
		{
			var type = typeof(T);
			return (T)CreateByFactory(type);
		}

		public void Destroy(object instance)
		{
			var type = instance.GetType();
			if (_factories.TryGetValue(type, out var factory))
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
			if (instance is IDisposable disposable)
			{
				_disposables.Add(disposable);
			}
		}

		private void TryRegisterFactory(object instance)
		{
			if (instance is IFactory factory)
			{
				var genericArguments = factory.GetType().GetGenericArguments();
				if(genericArguments.Length > 0)
				{
					var baseType = genericArguments[0];
					if (_factories.ContainsKey(baseType))
					{
						throw new Exception($"#DI# Type {baseType} already has factory.");
					}

					_factories[baseType] = factory;
					
					// Sub types
					var subTypes = Assembly
						.GetAssembly(baseType)
						.GetTypes()
						.Where(t => t.IsSubclassOf(baseType));
					foreach (var type in subTypes)
					{
						_factories[type] = factory;
					}
				}
			}
		}
		
		private object[] GetInjectMethodParameters(MethodBase injectMethod, object[] parameters)
		{
			var parameterInfos = DICache.GetMethodParameters(injectMethod);

			var paramArr = new object[parameterInfos.Length];
			for (var i = 0; i < parameterInfos.Length; i++)
			{
				var parameterInfo = parameterInfos[i];

				object resolved = null;

				if (parameters != null && parameters.Length > 0)
				{
					resolved = Array.Find(parameters, p =>
					{
						var pType = p.GetType();
						if(pType == parameterInfo.ParameterType)
							return true;
						
						// down
						if (Array.Exists(pType.GetInterfaces(), iType => iType == parameterInfo.ParameterType))
							return true;
						
						// up
						var bType = pType.BaseType;
						while (bType != null)
						{
							if (bType == parameterInfo.ParameterType)
								return true;
							bType = bType.BaseType;
						}

						return false;
					});
				}

				if (resolved == null)
				{
					var genericResolveMethod = DICache.GetTypedResolveMethod(parameterInfo.ParameterType);
					if (genericResolveMethod == null)
						throw new Exception($"#DI# Generic resolve method not found for {parameterInfo.ParameterType}");
					
					resolved = genericResolveMethod.Invoke(this, null);
				}

				paramArr[i] = resolved;
			}

			return paramArr;
		}

		#endregion
	}
}