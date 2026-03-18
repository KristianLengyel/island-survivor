using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class BuildingTileButton : MonoBehaviour
{
	[SerializeField] private Image tileImage;
	[SerializeField] private GameObject selectedHighlight;

	private static readonly Color AffordableColor = Color.white;
	private static readonly Color UnaffordableColor = new Color(0.4f, 0.4f, 0.4f, 1f);

	private TileBase tile;
	private BuildingMenuManager menuManager;

	public void Initialize(TileBase tileBase, Sprite sprite, BuildingMenuManager manager)
	{
		tile = tileBase;
		menuManager = manager;

		var btn = GetComponent<Button>();
		btn.onClick = new Button.ButtonClickedEvent();
		btn.onClick.AddListener(OnButtonClick);

		if (tileImage != null)
		{
			tileImage.sprite = sprite;
			tileImage.color = new Color(1f, 1f, 1f, sprite != null ? 1f : 0f);
		}

		SetSelected(false);
	}

	private void Update()
	{
		if (tile == null || tileImage == null) return;
		var bm = GameManager.Instance != null ? GameManager.Instance.BuildingManager : null;
		if (bm == null) return;

		float a = tileImage.color.a;
		Color target = bm.HasEnoughResources(tile) ? AffordableColor : UnaffordableColor;
		target.a = a;
		tileImage.color = target;
	}

	public TileBase GetTile() => tile;

	public void OnButtonClick()
	{
		if (menuManager == null || tile == null) return;
		menuManager.SelectButton(this);
	}

	public void SetSelected(bool selected)
	{
		if (selectedHighlight != null)
			selectedHighlight.SetActive(selected);
	}
}
