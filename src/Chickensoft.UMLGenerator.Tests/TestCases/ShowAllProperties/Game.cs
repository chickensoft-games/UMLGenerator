namespace Chickensoft.UMLGenerator.Tests;

using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Chickensoft.UMLGenerator;
using Godot;

[ClassDiagram(ShowAllProperties = true)]
[Meta(typeof(IAutoNode))]
public partial class Game : Node, IProvide<IGameRepo>
{
	#region Provisions
	
	IGameRepo IProvide<IGameRepo>.Value() => GameRepo;
	
	#endregion

	#region State

	public IGameRepo GameRepo { get; set; } = null!;
	public IGameLogic GameLogic { get; set; } = null!;
	public LogicBlock<GameLogic.State>.IBinding AppBinding { get; set; } = null!;
	
	#endregion

	#region Lifecycle

	public override void _Notification(int what) => this.Notify(what);

	public void Setup()
	{
		GameRepo = new GameRepo();
		GameRepo.CreateInstantiator(new Instantiator(GetTree()));
		GameLogic = new GameLogic();
		GameLogic.Set(GameRepo);
		GameLogic.Set(new GameLogic.Data());
	}

	public void OnResolved()
	{
		AppBinding = GameLogic.Bind();
		GameLogic.Start();
		this.Provide();
	}

	public void OnExitTree()
	{
		
	}

	#endregion
}