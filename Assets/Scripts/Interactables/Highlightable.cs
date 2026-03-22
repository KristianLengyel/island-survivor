using UnityEngine;

public class Highlightable : MonoBehaviour
{
	[SerializeField] private SpriteRenderer[] renderers;

	private static readonly int OutlineEnabledId = Shader.PropertyToID("_OutlineEnabled");

	private MaterialPropertyBlock _block;
	private bool _highlighted;

	private void Awake()
	{
		_block = new MaterialPropertyBlock();
	}

	public void SetHighlight(bool on)
	{
		if (_highlighted == on) return;
		_highlighted = on;
		for (int i = 0; i < renderers.Length; i++)
		{
			if (renderers[i] == null) continue;
			if (renderers[i].gameObject != gameObject)
			{
				renderers[i].gameObject.SetActive(on);
				renderers[i].enabled = on;
			}
			else
			{
				renderers[i].GetPropertyBlock(_block);
				_block.SetFloat(OutlineEnabledId, on ? 1f : 0f);
				renderers[i].SetPropertyBlock(_block);
			}
		}
	}

	public bool IsHighlighted => _highlighted;
}
