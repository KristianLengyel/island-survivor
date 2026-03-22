using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class BuildingTileButton : MonoBehaviour
{
	[SerializeField] private Image tileImage;
	[SerializeField] private GameObject selectedHighlight;

	private TileBase tile;

	public void Initialize(TileBase tileBase, Sprite sprite)
	{
		tile = tileBase;

		if (tileImage != null)
		{
			tileImage.sprite = sprite;
			tileImage.color = new Color(1f, 1f, 1f, sprite != null ? 1f : 0f);
		}

		SetSelected(false);
	}

	public TileBase GetTile() => tile;

	public void SetSelected(bool selected)
	{
		if (selectedHighlight != null)
			selectedHighlight.SetActive(selected);
	}
}
