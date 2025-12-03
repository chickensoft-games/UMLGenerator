namespace Chickensoft.UMLGenerator.Modules;

using System.Collections.Generic;
using System.Linq;
using Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models;

public class MethodModule : IModule
{
	public int Order => (int)ModuleOrder.Last;
	public string Title => "[Methods]";
	public bool ShouldDrawChildren => false;

	public List<ModuleItem> SetupModule(BaseNode node, IDictionary<string, BaseNode> sceneNodeList)
	{
		var baseTypeSyntax = node.TypeSyntax;
		var interfaceMethods = node.InterfaceSyntax?.Members.OfType<MethodDeclarationSyntax>() ?? [];
		if (baseTypeSyntax == null)
			return [];

		var methodDeclarations =
			from typeMember in baseTypeSyntax.Members
			where typeMember is MethodDeclarationSyntax typeMethod &&
			      (typeMethod.ExplicitInterfaceSpecifier?.Name as GenericNameSyntax)?.Identifier.Text != "IProvide" &&
			      interfaceMethods.All(x => x.Identifier.Value != typeMethod.Identifier.Value)
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