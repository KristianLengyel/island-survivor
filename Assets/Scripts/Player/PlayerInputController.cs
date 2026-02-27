using UnityEngine;

public class PlayerInputController : MonoBehaviour
{
	public Vector2 MovementInput { get; private set; }

	public void Initialize()
	{
		MovementInput = Vector2.zero;
	}

	public void Tick()
	{
		MovementInput = GameInput.Move.normalized;
	}

	public void SetBlocked()
	{
		MovementInput = Vector2.zero;
	}
}
