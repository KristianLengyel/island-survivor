using UnityEngine;

public class PlayerAnimationDriver : MonoBehaviour
{
	private Animator animator;
	private PlayerInputController input;
	private PlayerTileDetector tileDetector;

	public void Initialize(Animator animator, PlayerInputController input, PlayerTileDetector tileDetector)
	{
		this.animator = animator;
		this.input = input;
		this.tileDetector = tileDetector;
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

		if (move != Vector2.zero)
		{
			if (Mathf.Abs(move.x) > Mathf.Abs(move.y))
			{
				animator.SetFloat("lastMoveX", Mathf.Sign(move.x));
				animator.SetFloat("lastMoveY", 0f);
			}
			else
			{
				animator.SetFloat("lastMoveX", 0f);
				animator.SetFloat("lastMoveY", Mathf.Sign(move.y));
			}
		}
	}
}
