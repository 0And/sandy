using Godot;

public class Bullet : KinematicBody
{

	public const float InitialBulletSpeed = 50;
	public const float Gravity = -30;
	public const float BlastRadius = 5;
	public const float BlastSpeed = 30;
	public const float MinVerticalDistance = -200;

	private World _world;
	private Character _character;
	private Particles _particles;
	private Particles _explosion;
	private bool _hit = false;
	private Vector3 _velocity = new Vector3();

	public override void _Ready()
	{
		_world = GetNode<World>("..");
		_character = GetNode<Character>("../Character");
		_particles = GetNode<Particles>("Particles");
		_explosion = GetNode<Particles>("Explosion");
		_velocity = -GlobalTransform.basis.z * InitialBulletSpeed;
	}

	public override void _PhysicsProcess(float delta)
	{
		// Always translate Visibility AABB in character's viewport so particles don't get culled.
		// The particles animations only run while being rendered; this is to make sure the particles animations are always running.
		// This hack can be fixed by forcing the particles to render regardless of culling.
		// I am currently unsure of a way to disable the culling on a specific node.
		Vector3 localCharacterPosition = _particles.ToLocal(_character.GlobalTransform.origin);
		_particles.VisibilityAabb = new AABB(localCharacterPosition - _particles.VisibilityAabb.Size / 2, _particles.VisibilityAabb.Size);
		if (_hit)
		{
			// Always translate Visibility AABB in character's viewport for the same reason as above.
			localCharacterPosition = _explosion.ToLocal(_character.GlobalTransform.origin);
			_explosion.VisibilityAabb = new AABB(localCharacterPosition - _explosion.VisibilityAabb.Size / 2, _explosion.VisibilityAabb.Size);
			if (!_explosion.Emitting)
			{
				if (_particles.Emitting)
				{
					_explosion.Emitting = true;
					_particles.Emitting = false;
				}
				else
				{
					QueueFree();
				}
			}
		}
		else
		{
			GlobalTransform = GlobalTransform.LookingAt(GlobalTransform.origin + _velocity * delta, new Vector3(0, 1, 0));
			KinematicCollision collision = MoveAndCollide(_velocity * delta);
			if (collision != null)
			{
				if (collision.Position.DistanceTo(_character.GlobalTransform.origin) <= BlastRadius)
				{
					if (((Node)collision.Collider).IsInGroup("Block"))
					{
						_world.PlayerHitBlock(((Block)collision.Collider).BlockNumber);
					}
					Vector3 newVelocity = collision.Position.DirectionTo(_character.GlobalTransform.origin) * BlastSpeed;
					_character.ExternalVelocity = newVelocity;
				}
				_hit = true;
			}
			else if (GlobalTransform.origin.y <= MinVerticalDistance)
			{
				_hit = true;
			}
			else
			{
				_velocity.y += Gravity * delta;
			}
		}

	}

}
