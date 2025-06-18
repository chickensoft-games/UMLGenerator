namespace Chickensoft.UMLGenerator;

using System;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ClassDiagramAttribute : Attribute
{
	/// <summary>
	/// Changes the paths so that they're generated as full paths and uses the
	/// vscode:// url protocol. This allows them to be used with the VSCode plugin.
	/// </summary>
	public bool UseVSCodePaths { get; set; }
}