namespace Chickensoft.UMLGenerator.PumlModules;

using System.Collections.Generic;
using System.Linq;
using Helpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models;

public class MethodModule : IModule
{
	public int Order => (int)ModuleOrder.Last;
	public string Title => "[Methods]";
	public void SetupModule(BaseHierarchy hierarchy, IDictionary<string, BaseHierarchy> nodeHierarchyList)
	{
		var baseTypeSyntax = hierarchy.TypeSyntax;
		if (baseTypeSyntax == null)
			return;

		var methodDeclarations = baseTypeSyntax
			.Members.OfType<MethodDeclarationSyntax>()
			.Where(syntax => (syntax.ExplicitInterfaceSpecifier?.Name as GenericNameSyntax)?.Identifier.Text != "IProvide").ToList();
		var items = new List<ModuleItem>();

		foreach (var ctx in methodDeclarations)
		{
			items.Add(new ModuleItem
			{
				Name = ctx.Identifier.ToString(),
				LineNumber = ctx.GetLineNumber()
			});
		}
		if(items.Any())
			hierarchy.AddModule<MethodModule>(items);
	}

	public IEnumerable<string> InvokeModule(BaseHierarchy hierarchy, bool useVSCodePaths, int depth)
	{
		var parentScriptPath = hierarchy.GetScriptPath(useVSCodePaths, depth);
		var items = hierarchy.ModuleItems[typeof(MethodModule)] ?? [];
		foreach (var module in items)
		{
			yield return $"[[{parentScriptPath}:{module.LineNumber} {module.Name}()]]";
		}
	}
}