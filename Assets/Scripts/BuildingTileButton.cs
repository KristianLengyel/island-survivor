using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class BuildingTileButton : MonoBehaviour
{
	public TileBase tile;
	public Sprite tileSprite;
	public Image tileImage;

	private void Start()
	{
		if (tileSprite != null)
		{
			tileImage.sprite = tileSprite;
			tileImage.color = new Color(tileImage.color.r, tileImage.color.g, tileImage.color.b, 1f);
		}
		else
		{
			tileImage.sprite = null;
			tileImage.color = new Color(tileImage.color.r, tileImage.color.g, tileImage.color.b, 0f);
		}
	}

	public void OnButtonClick()
	{
		GameManager.Instance.BuildingManager.SetSelectedTile(tile);
	}
}
