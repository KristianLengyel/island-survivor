using UnityEngine;

public static class InteractableUtil
{
	public static bool IsMouseOverBounds(SpriteRenderer sr, Vector2 mouseWorldPos)
	{
		if (sr == null || sr.sprite == null) return false;
		var b = sr.bounds;
		return mouseWorldPos.x >= b.min.x && mouseWorldPos.x <= b.max.x &&
			   mouseWorldPos.y >= b.min.y && mouseWorldPos.y <= b.max.y;
	}

	public static void ResolveOutlineRenderer(Transform parent, ref SpriteRenderer outlineRenderer, out GameObject outlineChild)
	{
		if (outlineRenderer == null)
		{
			outlineChild = parent.Find("Outline")?.gameObject;
			if (outlineChild != null) outlineRenderer = outlineChild.GetComponent<SpriteRenderer>();
		}
		else
		{
			outlineChild = outlineRenderer.gameObject;
		}
		if (outlineChild != null && !outlineChild.activeSelf) outlineChild.SetActive(true);
		if (outlineRenderer != null) outlineRenderer.enabled = false;
	}
}
