namespace Chickensoft.UMLGenerator.Models.Godot;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

[DebuggerDisplay("{Name,nq}")]
public class Node
{
    public string Name { get; }
    public string Type { get; }
    public string? ParentPath { get; }
    public HashSet<string> Groups { get; }
    public Node? Parent {  get; }
    public List<Node> Children { get; }
    public Script? Script { get; }
    public Node(string name, string type, Node? parent, string? parentPath,
        Script? script, HashSet<string>? groups = null)
    {
        Name = name;
        Type = type;
        Parent = parent;
        ParentPath = parentPath;
        Groups = groups ?? new HashSet<string>();
        Children= new List<Node>();
        Script = script;
    }

    public string FullName => !string.IsNullOrWhiteSpace(ParentPath) && ParentPath != "."
        ? $"{ParentPath}/{Name}": Name;

    public Node? SelectChild(string path)
    {
        var segments = path.Split('/');
        return SelectChild(segments, 0);
    }
    public Node? SelectChild(string[] pathSegments, int index)
    {
        var child = Children
            .FirstOrDefault(c => string.Equals(c.Name, pathSegments[index], StringComparison.Ordinal));
        if (child is null)
        {
            return null;
        }
        index++;
        if (pathSegments.Length == index)
        {
            return child;
        }
        return child.SelectChild(pathSegments, index);
    }

    public IEnumerable<Node> AllChildren
    {
        get
        {
            foreach (var c in Children )
            {
                yield return c;
                foreach (var grandChild in c.AllChildren)
                {
                    yield return grandChild;
                }
            }
        }
    }
}