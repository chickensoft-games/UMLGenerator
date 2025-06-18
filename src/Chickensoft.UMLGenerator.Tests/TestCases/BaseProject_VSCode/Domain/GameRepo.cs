namespace Chickensoft.UMLGenerator.Tests;

using System;
using Chickensoft.Collections;
using Godot;

public interface IGameRepo : IDisposable
{
	IInstantiator Instantiator { get; }
	void CreateInstantiator(IInstantiator instantiator);
}

public class GameRepo : IGameRepo
{
	public IInstantiator Instantiator { get; private set; } = null!;

	public void CreateInstantiator(IInstantiator instantiator)
	{
		if (Instantiator != null)
			throw new ArgumentException("Instantiator cannot be set more than once", nameof(instantiator));
		Instantiator = instantiator ?? throw new ArgumentNullException(nameof(instantiator));
	}

	public void Dispose()
	{
		
	}
}