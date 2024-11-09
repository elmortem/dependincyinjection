# Primitive Dependency Injection for Unity
[![color:ff69b4](https://img.shields.io/badge/licence-Unlicense-blue)](https://unlicense.org)
![color:ff69b4](https://img.shields.io/badge/Unity-2019.3.x-red)

The primitive dependency injection system for prototypes assembled on the knee.

## Installation
Installation as a unity module via a git link in PackageManager:
```
https://github.com/elmortem/dependincyinjection.git?path=Packages/dependincyinjection
```

Or direct editing of `Packages/manifest` is supported.json:
```
"com.elmortem.dependincyinjection": "https://github.com/elmortem/dependincyinjection.git?path=Packages/dependincyinjection",
```

## Main types
### InjectAttribute
Mark construtor or method with them if you want to inject parameters to instance.

#### Example:
```
public class LevelManager
{
	private readonly AssetSystem _assetSystem;

	[Inject]
	public LevelManager(AssetSystem assetSystem)
	{
		_assetSystem = assetSystem;
	}
}
```
```
public class Movement : MonoBehaviour
{
	private MovementSystemConfig _movementSystemConfig;

	[Inject]
	public void Construct(MovementSystemConfig movementSystemConfig)
	{
		_movementSystemConfig = movementSystemConfig;
	}
}
```

### DIContainer
DI container class.

#### Example:
```
DIContainer.Root.BindAsSingle<LevelManager>();
```

### IDIMaker
Base DI interface.

#### Example:
```
public class AssetSystem
{
	private readonly IDIMaker _maker;

	[Inject]
	public AssetSystem(IDIResolver resolver)
	{
		_maker = maker;
	}
	
	public AssetLoader MakeLoader()
	{
		return _maker.Create<AssetLoader>();
	}
}
```

## Info
Collected from pieces of code on the Internet and various tutorials, completed with love for use in jams.

Support Unity 2019.3 or later.

Use for free.

Enjoy!
