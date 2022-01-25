using Godot;

public class Gui : Control
{
	public Color NumberColor = new Color(0.854902f, 0.905882f, 0.905882f); // White: #dae7e7

	public const float NumberTextureRatio = 0.6f;
	public const float Number1TextureRatio = 0.3f;
	public const float NumberTextureSpacing = 0.1f;

	private Control _gameScreen;
	private Control _pauseScreen;
	private HBoxContainer _scoreHBox;
	private HBoxContainer _highScoreHBox;
	private Texture[] _numberTextures = new Texture[10];

	public void AddNumbersToHBox(HBoxContainer hBox, int number)
	{
		foreach (Node node in hBox.GetChildren())
		{
			node.QueueFree();
		}
		string numberString = number.ToString();
		int digitCount = numberString.Length();
		float numberHBoxRatio = NumberTextureSpacing;
		for (int i = 0; i < digitCount; i++)
		{
			int digit = (int) (numberString[i] - '0');
			float ratio = digit == 1 ? Number1TextureRatio : NumberTextureRatio;
			TextureRect texture = new TextureRect();
			texture.Texture = _numberTextures[digit];
			texture.Modulate = NumberColor;
			texture.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
			texture.SizeFlagsHorizontal = (int) SizeFlags.ExpandFill;
			texture.SizeFlagsStretchRatio = ratio / NumberTextureRatio;
			hBox.AddChild(texture);
			numberHBoxRatio += ratio + NumberTextureSpacing;
		}
		hBox.GetParent<AspectRatioContainer>().Ratio = numberHBoxRatio;
	}

	public void UpdateScore(int score)
	{
		AddNumbersToHBox(_scoreHBox, score);
	}

	public void UpdateHighScore(int highScore)
	{
		AddNumbersToHBox(_highScoreHBox, highScore);
	}

	public void PauseToggle()
	{
		bool paused = !GetTree().Paused;
		GetTree().Paused = paused;
		_pauseScreen.Visible = paused;
		if (paused)
		{
			Input.SetMouseMode(Input.MouseMode.Visible);
		}
		else
		{
			Input.SetMouseMode(Input.MouseMode.Captured);
		}
	}

	public override void _Ready()
	{
		for (int i = 0; i < _numberTextures.Length; i++)
		{
			_numberTextures[i] = GD.Load<Texture>($"res://Textures/{i.ToString()}.png");
		}
		_gameScreen = GetNode<Control>("GameScreen");
		_pauseScreen = GetNode<Control>("PauseLayer/PauseScreen");
		_scoreHBox = GetNode<HBoxContainer>("GameScreen/ScoreContainer/HBox");
		_highScoreHBox = GetNode<HBoxContainer>("GameScreen/BestContainer/HBox");
	}

	public override void _Process(float delta)
	{
		if (Input.IsActionJustPressed("pause"))
		{
			PauseToggle();
		}
	}
}
