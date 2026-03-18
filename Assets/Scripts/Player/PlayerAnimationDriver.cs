using UnityEngine;

public class PlayerAnimationDriver : MonoBehaviour
{
	private Animator animator;
	private PlayerInputController input;
	private PlayerTileDetector tileDetector;

	private bool _facingOverride;
	private Vector2 _facingDir;

	public void Initialize(Animator animator, PlayerInputController input, PlayerTileDetector tileDetector)
	{
		this.animator = animator;
		this.input = input;
		this.tileDetector = tileDetector;
	}

	public void SetFacingDirection(Vector2 dir)
	{
		_facingOverride = true;
		_facingDir = dir;
	}

	public void ClearFacingDirection()
	{
		_facingOverride = false;
		_facingDir = Vector2.zero;
	}

	public void Tick()
	{
		if (!animator || input == null) return;

		Vector2 move = input.MovementInput;

		animator.SetFloat("moveX", move.x);
		animator.SetFloat("moveY", move.y);
		animator.SetFloat("moveAmount", move.magnitude);

		if (tileDetector != null)
		{
			animator.SetBool("isSwimming", tileDetector.IsInWater());
		}

		Vector2 faceSource = _facingOverride && _facingDir != Vector2.zero ? _facingDir : move;

		if (faceSource != Vector2.zero)
		{
			if (Mathf.Abs(faceSource.x) > Mathf.Abs(faceSource.y))
			{
				animator.SetFloat("lastMoveX", Mathf.Sign(faceSource.x));
				animator.SetFloat("lastMoveY", 0f);
			}
			else
			{
				animator.SetFloat("lastMoveX", 0f);
				animator.SetFloat("lastMoveY", Mathf.Sign(faceSource.y));
			}
		}
	}
}
