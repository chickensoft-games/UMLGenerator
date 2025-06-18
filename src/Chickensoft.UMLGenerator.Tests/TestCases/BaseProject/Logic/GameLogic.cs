namespace Chickensoft.UMLGenerator.Tests;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

public interface IGameLogic : ILogicBlock<GameLogic.State>;

[Meta]
[LogicBlock(typeof(State), Diagram = true)]
public partial class GameLogic : LogicBlock<GameLogic.State>, IGameLogic
{
	public override Transition GetInitialState() => To<State>();

	public record Data;

	public static class Input
	{
		public readonly record struct NewGame;
	}

	public static class Output
	{
	}

	[Meta]
	public partial record State : StateLogic<State>;
}