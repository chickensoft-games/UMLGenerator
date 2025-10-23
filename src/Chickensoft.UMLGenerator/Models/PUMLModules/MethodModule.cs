namespace Chickensoft.UMLGenerator.PumlModules;

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
	public List<ModuleItem> SetupModule(BaseHierarchy hierarchy, IDictionary<string, BaseHierarchy> nodeHierarchyList)
	{
		var baseTypeSyntax = hierarchy.TypeSyntax;
		var hierarchyContext = hierarchy.SemanticModel;
		var interfaceMethods = hierarchy.InterfaceSyntax?.Members.OfType<MethodDeclarationSyntax>() ?? [];
		if (baseTypeSyntax == null)
			return [];

		var methodDeclarations =
			from typeMember in baseTypeSyntax.Members
			where typeMember is MethodDeclarationSyntax typeMethod &&
			      (typeMethod.ExplicitInterfaceSpecifier?.Name as GenericNameSyntax)?.Identifier.Text != "IProvide" &&
			      interfaceMethods.All(x => x.Identifier.Value != typeMethod.Identifier.Value) &&
			      hierarchyContext?.GetDeclaredSymbol(typeMethod) is { IsOverride: false }
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

	public IEnumerable<string> InvokeModule(BaseHierarchy hierarchy, List<ModuleItem> moduleItems, bool useVSCodePaths, int depth)
	{
		var parentScriptPath = hierarchy.GetScriptPath(useVSCodePaths, depth);
		foreach (var module in moduleItems)
		{
			yield return $"[[{parentScriptPath}:{module.LineNumber} {module.Name}()]]";
		}
	}
}