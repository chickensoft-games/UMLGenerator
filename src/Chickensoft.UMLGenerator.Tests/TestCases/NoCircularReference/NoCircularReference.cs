using System;
using System.Threading.Tasks;
using Godot;
using GC = Godot.Collections;

[ClassDiagram]
public partial class NoCircularReference : Node
{
    public static NoCircularReference Inst { get; private set; }
}
