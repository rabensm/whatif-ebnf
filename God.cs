using Godot;
using System.Diagnostics;

public partial class God : Node
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Debug.WriteLine("Ready method called.");
		Grammar.GrammarTree gt = new ("wif.ebnf");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
