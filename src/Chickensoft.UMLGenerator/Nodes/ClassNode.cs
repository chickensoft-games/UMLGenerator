namespace Chickensoft.UMLGenerator.Models;

using System.Collections.Generic;
using System.Linq;
using Helpers;
using Microsoft.CodeAnalysis;

public class ClassNode(IGrouping<string, GeneratorSyntaxContext> contextGrouping, GenerationData data) : BaseNode(data)
{
	public override List<GeneratorSyntaxContext> ContextList { get; } = contextGrouping.ToList();
	public override string FullFilePath => contextGrouping.Key.NormalizePath();
	public override string FullScriptPath => FullFilePath;
}