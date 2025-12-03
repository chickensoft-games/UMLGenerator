namespace Chickensoft.UMLGenerator.Modules;

using System.Collections.Generic;
using System.Linq;
using Helpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models;

public class InterfaceMethodModule : IModule
{
	public int Order => (int)ModuleOrder.Last;
	public string Title => "[Interface Methods]";
	public bool ShouldDrawChildren => false;

	public List<ModuleItem> SetupModule(BaseNode node, IDictionary<string, BaseNode> sceneNodeList)
	{
		var baseTypeSyntax = node.TypeSyntax;
		if (baseTypeSyntax == null)
			return [];

		var methodDeclarations =
			from interfaceMember in node.InterfaceSyntax?.Members ?? []
			from typeMember in node.TypeSyntax?.Members ?? []
			where typeMember is MethodDeclarationSyntax typeMethod &&
			      interfaceMember is MethodDeclarationSyntax interfaceMethod &&
			      typeMethod.Identifier.Value == interfaceMethod.Identifier.Value
			orderby (typeMember as MethodDeclarationSyntax)?.Identifier.ValueText
			select typeMember as MethodDeclarationSyntax;

		var items = new List<ModuleItem>();

		foreach (var ctx in methodDeclarations)
		{
			items.Add(new ModuleItem
			{
				Name = ctx.Identifier.ToString(),
				LineNumber = ctx.GetLineNumber()
			});
		}

		return items;
	}

	public IEnumerable<string> InvokeModule(BaseNode node, List<ModuleItem> moduleItems, bool useVSCodePaths, int depth)
	{
		var parentScriptPath = node.GetScriptPath(useVSCodePaths, depth);
		foreach (var module in moduleItems)
		{
			yield return $"[[{parentScriptPath}:{module.LineNumber} {module.Name}()]]";
		}
	}
}