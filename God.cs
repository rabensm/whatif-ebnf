using Godot;
using System.IO;

public partial class God : Node
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Grammar.GrammarTree gt = new ();

		FileInfo testWifFile = new("itest.wif");
		string testWif = File.ReadAllText(testWifFile.FullName);
		gt.Parse(testWif);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
