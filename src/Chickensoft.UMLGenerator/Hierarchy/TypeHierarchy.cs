namespace Chickensoft.UMLGenerator.Models;

using System.Collections.Generic;
using System.Linq;
using Helpers;
using Microsoft.CodeAnalysis;

public class TypeHierarchy(IGrouping<string, GeneratorSyntaxContext> contextGrouping, GenerationData data) : BaseHierarchy(data)
{
	public override List<GeneratorSyntaxContext> ContextList { get; } = contextGrouping.ToList();
	public override string FullFilePath => contextGrouping.Key.NormalizePath();
	public override string FullScriptPath => FullFilePath;
}