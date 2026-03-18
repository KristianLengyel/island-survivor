using UnityEngine;

public class Highlightable : MonoBehaviour
{
	[SerializeField] private SpriteRenderer[] outlineRenderers;

	private bool _highlighted;

	public void SetHighlight(bool on)
	{
		if (_highlighted == on) return;
		_highlighted = on;
		for (int i = 0; i < outlineRenderers.Length; i++)
		{
			if (outlineRenderers[i] == null) continue;
			outlineRenderers[i].gameObject.SetActive(on);
		}
	}

	public bool IsHighlighted => _highlighted;
}
