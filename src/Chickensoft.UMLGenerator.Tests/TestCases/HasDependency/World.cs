namespace Chickensoft.UMLGenerator.Tests;

using BG4G.Core.Extensions;
using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

public interface IWorld : INode3D
{
	public IWorldRepo Repo { get; set; }
}

[SceneTree]
[Meta(typeof(IAutoNode))]
public partial class World : Node3D, IWorld,
{
	[Dependency] public IGameRepo GameRepo => this.DependOn<IGameRepo>();
}