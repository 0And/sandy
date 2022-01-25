using Godot;

public class World : Spatial
{
	[Signal]
    public delegate void ScoreUpdated(int score);
	[Signal]
    public delegate void HighScoreUpdated(int highScore);

	private PackedScene blockScene = GD.Load<PackedScene>("res://Components/Block.tscn");

	public const string SaveFilePath = "user://loader.dat";
	public const string SaveFilePassword = "ca4c15cc-97d2-4cb8-8fd7-fc01d1d0c236"; // Security vulnerability.
	public const int InitialBlocksAmount = 15;
	public const float BlocksDistance = 25;
	public const float BlockSideLimits = 10; // This value cannot exceed half the distance of BlocksDistance, will clamp.

	private Gui _gui;
	private Spatial _blocks;
	private int _score = 0;
	private int _highScore = 0;
	private int _blocksAmount = 0;
	Vector3 _lastBlockPos = new Vector3(0, 0, 0);

	private Curve[] _islandCurves;

	public void PlayerHitBlock(int pBlockNumber)
	{
		if (pBlockNumber > _score)
		{
			_score = pBlockNumber;
			EmitSignal(nameof(ScoreUpdated), _score);
			AddBlocks();
			if (_score > _highScore)
			{
				_highScore = _score;
				Save(_highScore);
				EmitSignal(nameof(HighScoreUpdated), _score);
			}
		}
	}

	public void PlayerReset()
	{
		_score = 0;
		EmitSignal(nameof(ScoreUpdated), _score);
		InitiateBlocks();
	}

	private void Save(int score)
	{
		File file = new File();
		file.OpenEncryptedWithPass(SaveFilePath, File.ModeFlags.Write, SaveFilePassword);
		file.StoreString(score.ToString());
		file.Close();
	}

	private void Load()
	{
		File file = new File();
		if (file.FileExists(SaveFilePath))
		{
			file.OpenEncryptedWithPass(SaveFilePath, File.ModeFlags.Read, SaveFilePassword);
			string content = file.GetAsText();
			file.Close();
			int.TryParse(content, out _highScore);
		}
	}

	private void AddBlocks()
	{
		int newBlocksAmount = _score + InitialBlocksAmount - _blocksAmount;
		for (int i = 0; i < newBlocksAmount; i++)
		{
			Block block = blockScene.Instance<Block>();
			// BlockSideLimits cannot exceed half the distance of each block.
			float realBlockSideLimits = Mathf.Clamp(BlockSideLimits, 0, BlocksDistance / 2);
			float leftMaxXDistance = -realBlockSideLimits - _lastBlockPos.x;
			float rightMaxXDistance = realBlockSideLimits - _lastBlockPos.x;
			float leftMaxZDistance = -Mathf.Sqrt(BlocksDistance * BlocksDistance - leftMaxXDistance * leftMaxXDistance);
			float rightMaxZDistance = -Mathf.Sqrt(BlocksDistance * BlocksDistance - rightMaxXDistance * rightMaxXDistance);
			Vector3 leftVector = new Vector3(leftMaxXDistance, 0, leftMaxZDistance);
			Vector3 rightVector = new Vector3(rightMaxXDistance, 0, rightMaxZDistance);
			// Must do a custom slerp because of a C# bug in Godot.
			float randomAngle = leftVector.AngleTo(rightVector) * (float) GD.RandRange(0, 1);
			block.Translation = _lastBlockPos + leftVector.Rotated(leftVector.Cross(rightVector).Normalized(), randomAngle);
			_lastBlockPos = block.Translation;
			block.BlockNumber = ++_blocksAmount;
			Connect("ScoreUpdated", block, nameof(block.UpdateScore));
			block.GenerateIsland(_islandCurves);
			_blocks.AddChild(block);
		}
	}

	private void InitiateBlocks()
	{
		foreach (Node block in _blocks.GetChildren())
		{
			block.QueueFree();
		}
		_lastBlockPos = new Vector3();
		_blocksAmount = 0;
		Block baseBlock = blockScene.Instance<Block>();
		baseBlock.GenerateIsland(_islandCurves, 0);
		_blocks.AddChild(baseBlock);
		AddBlocks();
	}

	private void CreateCurves()
	{
		_islandCurves = new Curve[]
		{
			new Curve(),
			new Curve(),
		};
		_islandCurves[0].AddPoint(new Vector2(0, 0.1f), 0, 0, Curve.TangentMode.Linear, Curve.TangentMode.Linear);
		_islandCurves[0].AddPoint(new Vector2(0.05f, 0.3f), 0, 0, Curve.TangentMode.Linear, Curve.TangentMode.Linear);
		_islandCurves[0].AddPoint(new Vector2(0.4f, 0.5f), 0, 0, Curve.TangentMode.Linear, Curve.TangentMode.Linear);
		_islandCurves[0].AddPoint(new Vector2(0.6f, 0.7f), 0, 0, Curve.TangentMode.Linear, Curve.TangentMode.Linear);
		_islandCurves[0].AddPoint(new Vector2(0.8f, 1), 0, 0, Curve.TangentMode.Linear, Curve.TangentMode.Linear);
		_islandCurves[0].AddPoint(new Vector2(0.9f, 1), 0, 0, Curve.TangentMode.Linear, Curve.TangentMode.Linear);
		_islandCurves[0].AddPoint(new Vector2(1, 0.5f), 0, 0, Curve.TangentMode.Linear, Curve.TangentMode.Linear);
		_islandCurves[1].AddPoint(new Vector2(0, 0.7f), 0, 0, Curve.TangentMode.Linear, Curve.TangentMode.Linear);
		_islandCurves[1].AddPoint(new Vector2(0.1f, 0.85f), 0, 0, Curve.TangentMode.Linear, Curve.TangentMode.Linear);
		_islandCurves[1].AddPoint(new Vector2(0.6f, 1), 0, 0, Curve.TangentMode.Linear, Curve.TangentMode.Linear);
		_islandCurves[1].AddPoint(new Vector2(0.85f, 1f), 0, 0, Curve.TangentMode.Linear, Curve.TangentMode.Linear);
		_islandCurves[1].AddPoint(new Vector2(0.95f, 0.8f), 0, 0, Curve.TangentMode.Linear, Curve.TangentMode.Linear);
		_islandCurves[1].AddPoint(new Vector2(1, 0.4f), 0, 0, Curve.TangentMode.Linear, Curve.TangentMode.Linear);
	}

	public override void _Ready()
	{
		// Initialize World
		// Load Save File
		Load();
		// Create Curve Points
		CreateCurves();
		// Randomize numbers
		GD.Randomize();

		_gui = GetNode<Gui>("Gui");
		Connect("ScoreUpdated", _gui, nameof(_gui.UpdateScore));
		Connect("HighScoreUpdated", _gui, nameof(_gui.UpdateHighScore));

		_blocks = GetNode<Spatial>("Blocks");
		InitiateBlocks();
		EmitSignal(nameof(ScoreUpdated), _score);
		EmitSignal(nameof(HighScoreUpdated), _highScore);
	}

}
