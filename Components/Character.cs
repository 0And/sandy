using Godot;

public class Character : KinematicBody
{

	private PackedScene bulletScene = GD.Load<PackedScene>("res://Components/Bullet.tscn");

	public const float RegularSpeed = 6;
	public const float FloatSpeed = 3;
	public const float Accel = 150;
	public const float Gravity = -45;
	public const float AirDrag = 5;
	public const float GroundDrag = 30;
	public const float JumpSpeed = 15;
	public const float TerminalVelocity = -100;
	public const float RotationAccel = 10;
	public const float BulletRotationAdjustment = 15;

	public const float MouseSensitivity = 0.1f;
	public const float MaxVerticalView = 75;
	public const float ApproxRotation = 0.9999f;

	public const float FadeWeight = 2;

	public Vector3 PlayerStartingOrigin = new Vector3(0, 20, 0);

	private Spatial _view;
	private Spatial _rotView;
	private CollisionShape _collision;
	private World _world;
	private AnimationTree _animTree;
	private ColorRect _fade;
	
	private Vector3 _externalVelocity = new Vector3();
	private Vector3 _velocity = new Vector3();
	private Vector3 _internalVelocity = new Vector3();
	private bool _floating = false;
	private bool _shooting = false;
	private Basis _rotateTo = new Basis();

	public const float AnimateAccel = 0.1f;
	public const int FallsToAnimate = 3;
	public Vector2 IdleAnim = new Vector2(0, 0);
	public Vector2 WalkAnim = new Vector2(1, 0);
	public Vector2 FallAnim = new Vector2(0, -1);
	private Vector2 _anim = new Vector2(0, 0);
	private int _falls = 0;

	public Vector3 ExternalVelocity
	{
		get { return _externalVelocity; }
		set { _externalVelocity = value; }
	}

	public void ResetCharacter()
	{
		_externalVelocity = new Vector3();
		_velocity = new Vector3();
		_internalVelocity = new Vector3();
		_floating = false;
		_shooting = false;
		_rotateTo = new Basis();
		GlobalTransform = new Transform(GlobalTransform.basis, PlayerStartingOrigin);
		_world.PlayerReset();
	}

	public override void _Ready()
	{
		_view = GetNode<Spatial>("View");
		_rotView = GetNode<Spatial>("View/RotView");
		_collision = GetNode<CollisionShape>("Collision");
		_world = GetNode<World>("..");
		_animTree = GetNode<AnimationTree>("AnimationTree");
		_fade = GetNode<ColorRect>("Fade");
		Input.SetMouseMode(Input.MouseMode.Captured);
	}

	public override void _PhysicsProcess(float delta)
	{

		// Fire Weapon
		if (Input.IsActionJustPressed("trigger"))
		{
			_rotateTo = _view.GlobalTransform.basis;
			_shooting = true;
			KinematicBody bullet = bulletScene.Instance<KinematicBody>();
			bullet.GlobalTransform = _rotView.GlobalTransform;
			bullet.RotationDegrees += new Vector3(BulletRotationAdjustment, 0, 0);
			_world.AddChild(bullet);
		}

		// Get User Movement Input
		Vector2 internalDirection = new Vector2();
		if (Input.IsActionPressed("move_right"))
		{
			internalDirection.x += 1;
		}
		if (Input.IsActionPressed("move_left"))
		{
			internalDirection.x -= 1;
		}
		if (Input.IsActionPressed("move_down"))
		{
			internalDirection.y += 1;
		}
		if (Input.IsActionPressed("move_up"))
		{
			internalDirection.y -= 1;
		}
		internalDirection = internalDirection.Normalized();

		// Add Character Rotation
		if (_shooting)
		{
			Quat initialRotation = _collision.GlobalTransform.basis.RotationQuat();
			Quat finalRotation = _rotateTo.RotationQuat();
			Basis finalCollisionBasis = new Basis(initialRotation.Slerp(finalRotation, delta * RotationAccel));
			_collision.GlobalTransform = new Transform(finalCollisionBasis, _collision.GlobalTransform.origin);
			if (_collision.GlobalTransform.basis.RotationQuat().Dot(_rotateTo.RotationQuat()) >= ApproxRotation)
			{
				_shooting = false;
			}
		}
		else if (internalDirection.Length() > 0)
		{
			Quat initialRotation = _collision.GlobalTransform.basis.RotationQuat();
			Vector3 finalRotationDirection = _view.GlobalTransform.origin;
			finalRotationDirection += _view.GlobalTransform.basis.x * internalDirection.x;
			finalRotationDirection += _view.GlobalTransform.basis.z * internalDirection.y;
			Quat finalRotation = _view.GlobalTransform.LookingAt(finalRotationDirection, new Vector3(0, 1, 0)).basis.RotationQuat();
			Basis finalCollisionBasis = new Basis(initialRotation.Slerp(finalRotation, delta * RotationAccel));
			_collision.GlobalTransform = new Transform(finalCollisionBasis, _collision.GlobalTransform.origin);
		}
		_collision.Orthonormalize();

		// Add Internal Velocity Acceleration
		float speed = _floating ? FloatSpeed : RegularSpeed;
		Vector3 internalVelocityGoal = new Vector3();
		internalVelocityGoal += _view.GlobalTransform.basis.x * internalDirection.x * speed;
		internalVelocityGoal += _view.GlobalTransform.basis.z * internalDirection.y * speed;
		_internalVelocity = new Vector3(_internalVelocity.x, 0, _internalVelocity.z);
		if (_internalVelocity.DistanceTo(internalVelocityGoal) > 0)
		{
			float accelWeight = Mathf.Clamp(Accel * delta / _internalVelocity.DistanceTo(internalVelocityGoal), 0, 1);
			_internalVelocity = _internalVelocity.LinearInterpolate(internalVelocityGoal, accelWeight);
		}
		else
		{
			_internalVelocity = internalVelocityGoal;
		}

		// Create New Velocity Variable
		Vector3 newVelocity = _velocity;

		// Jump and Gravity
		if (Input.IsActionPressed("jump") && IsOnFloor())
		{
			newVelocity.y += JumpSpeed;
		}
		else
		{
			// For no terminal velocity, use: newVelocity.y += Gravity * delta;
			if (newVelocity.y < TerminalVelocity)
			{
				newVelocity.y = Mathf.Min(newVelocity.y - Gravity * delta, TerminalVelocity);
			}
			else
			{
				newVelocity.y = Mathf.Max(newVelocity.y + Gravity * delta, TerminalVelocity);
			}
		}

		// Add External Velocity
		if (_externalVelocity.Length() > 0)
		{
			if (_externalVelocity.y != 0)
			{
				newVelocity.y = _externalVelocity.y;
				_externalVelocity.y = 0;
			}
			float drag = AirDrag;
			if (!_floating)
			{
				_floating = true;
			}
			else
			{
				if (GetSlideCount() > 0)
				{
					drag += GroundDrag;
					if (IsOnFloor() && _internalVelocity.Length() > 0 && RegularSpeed >= _internalVelocity.Length() + _externalVelocity.Length())
					{
						_internalVelocity.x += _externalVelocity.x;
						_internalVelocity.z += _externalVelocity.z;
						_externalVelocity = new Vector3();
						_floating = false;
					}
				}
			}
			newVelocity.x = _externalVelocity.x + _internalVelocity.x;
			newVelocity.z = _externalVelocity.z + _internalVelocity.z;
			float accelWeight = Mathf.Clamp(drag * delta / _externalVelocity.Length(), 0, 1);
			_externalVelocity = _externalVelocity.LinearInterpolate(new Vector3(), accelWeight);
		}
		else
		{
			if (!IsOnFloor())
			{
				_falls++;
			}
			else
			{
				_floating = false;
				if (_falls > 0)
				{
					_falls = 0;
				}
			}
			newVelocity.x = _internalVelocity.x;
			newVelocity.z = _internalVelocity.z;
			_externalVelocity = new Vector3();
		}

		// Character Animation
		Vector2 goal = IdleAnim;
		if (_floating || _falls >= FallsToAnimate)
		{
			goal = FallAnim;
		}
		else if (internalDirection.Length() > 0)
		{
			goal = WalkAnim;
		}
		float weight = Mathf.Clamp(AnimateAccel / _anim.DistanceTo(goal), 0, 1);
		_anim = _anim.LinearInterpolate(goal, weight);
		_animTree.Set("parameters/BlendSpace2D/blend_position", _anim);

		// Move Character
		_velocity = MoveAndSlide(newVelocity, new Vector3(0, 1, 0), true);

		//Check Collision
		for (int i = 0; i < GetSlideCount(); i++)
		{
			Node collider = (Node)GetSlideCollision(i).Collider;
			if ((collider).IsInGroup("Block"))
			{
				_world.PlayerHitBlock(((Block)collider).BlockNumber);
			}
		}

		// Reset Fallen Character
		float fadeAlpha = _fade.Modulate.a;
		if (GlobalTransform.origin.y <= 0)
		{
			_fade.Modulate = new Color(1, 1, 1, Mathf.Clamp(fadeAlpha + FadeWeight * delta, 0, 1));
			if (fadeAlpha >= 1)
			{
				ResetCharacter();
			}
		}
		else if (fadeAlpha > 0)
		{
			_fade.Modulate = new Color(1, 1, 1, Mathf.Clamp(fadeAlpha - FadeWeight * delta, 0, 1));
		}

	}

	public override void _Input(InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseMotion)
		{
			InputEventMouseMotion mouseEvent = inputEvent as InputEventMouseMotion;

			_view.RotateY(Mathf.Deg2Rad(-mouseEvent.Relative.x * MouseSensitivity));
			_view.Orthonormalize();
			_rotView.RotateX(Mathf.Deg2Rad(-mouseEvent.Relative.y * MouseSensitivity));

			Vector3 cameraRot = _rotView.RotationDegrees;
			cameraRot.x = Mathf.Clamp(cameraRot.x, -MaxVerticalView, MaxVerticalView);
			_rotView.RotationDegrees = cameraRot;
		}
	}

}
