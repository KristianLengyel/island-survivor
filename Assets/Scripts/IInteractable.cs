using UnityEngine;

public interface IInteractable
{
	void SetHighlighted(bool highlight);
	void Interact();
	float GetInteractionRange();
	Transform GetTransform();
	bool IsMouseOverSprite(Vector2 mouseWorldPos);
}