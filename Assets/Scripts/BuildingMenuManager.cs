using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class BuildingMenuManager : MonoBehaviour
{
	[SerializeField] private Item hammerItem;
	[SerializeField] private UIDocument uiDocument;

	private static readonly Color AffordableColor = Color.white;
	private static readonly Color UnaffordableColor = new Color(0.314f, 0.314f, 0.314f, 1f);
	private static readonly Color CostAffordableColor = new Color(1f, 1f, 1f, 0.85f);
	private static readonly Color CostUnaffordableColor = new Color(0.86f, 0.31f, 0.31f, 1f);

	private VisualElement overlay;
	private VisualElement btnContainer;
	private VisualElement costRow;
	private VisualElement previewIcon;
	private Label tileTypeLabel;
	private VisualElement selectedBtn;
	private TileResourceRequirement selectedRequirement;
	private bool isOpen;

	private struct ButtonEntry
	{
		public VisualElement btn;
		public VisualElement icon;
		public TileBase tile;
	}

	private struct CostEntry
	{
		public Label label;
		public ResourceRequirement req;
	}

	private readonly List<ButtonEntry> buttons = new List<ButtonEntry>();
	private readonly List<CostEntry> costLabels = new List<CostEntry>();

	public bool IsBuildingMenuOpen => isOpen;

	private void Start()
	{
		CacheUi();
		BuildButtons();
	}

	private void CacheUi()
	{
		if (uiDocument == null) return;
		var root = uiDocument.rootVisualElement;
		overlay = root.Q<VisualElement>("build-menu-overlay");
		btnContainer = root.Q<VisualElement>("build-btn-container");
		costRow = root.Q<VisualElement>("build-cost-row");
		tileTypeLabel = root.Q<Label>("build-tile-type");
		previewIcon = root.Q<VisualElement>("build-preview-icon");
	}

	private void BuildButtons()
	{
		if (btnContainer == null) return;

		var bm = GameManager.Instance != null ? GameManager.Instance.BuildingManager : null;
		if (bm == null) return;

		var reqs = bm.GetAllTileRequirements();
		foreach (var req in reqs)
		{
			if (req == null || req.tile == null) continue;
			if (bm.GetTileCategory(req.tile) == TileCategory.Pipe) continue;

			var btn = new VisualElement();
			btn.AddToClassList("build-tile-btn");

			var icon = new VisualElement();
			icon.AddToClassList("build-tile-icon");
			icon.pickingMode = PickingMode.Ignore;
			if (req.menuIcon != null)
				icon.style.backgroundImage = new StyleBackground(req.menuIcon);

			btn.Add(icon);
			btnContainer.Add(btn);

			var capturedBtn = btn;
			var capturedReq = req;
			btn.RegisterCallback<PointerDownEvent>(evt =>
			{
				if (evt.button != 0) return;
				SelectButton(capturedBtn, capturedReq);
			});

			buttons.Add(new ButtonEntry { btn = btn, icon = icon, tile = req.tile });
		}
	}

	private void Update()
	{
		if (GameInput.BuildMenuDown)
			MenuCoordinator.Instance.Toggle("BuildMenu");
		else if (GameInput.CancelDown && isOpen)
			CloseBuildingMenu();

		if (isOpen)
			UpdateAffordability();
	}

	private void UpdateAffordability()
	{
		var bm = GameManager.Instance != null ? GameManager.Instance.BuildingManager : null;
		if (bm == null) return;

		for (int i = 0; i < buttons.Count; i++)
		{
			var entry = buttons[i];
			entry.icon.style.unityBackgroundImageTintColor = bm.HasEnoughResources(entry.tile) ? AffordableColor : UnaffordableColor;
		}

		if (costLabels.Count == 0) return;

		var inv = GameManager.Instance.InventoryManager;
		for (int i = 0; i < costLabels.Count; i++)
		{
			var entry = costLabels[i];
			bool canAfford = inv.GetItemCount(entry.req.resource.name) >= entry.req.amount;
			entry.label.style.color = canAfford ? CostAffordableColor : CostUnaffordableColor;
		}
	}

	private void SelectButton(VisualElement btn, TileResourceRequirement req)
	{
		if (selectedBtn != null)
			selectedBtn.RemoveFromClassList("build-tile-btn--selected");

		selectedBtn = btn;
		selectedBtn.AddToClassList("build-tile-btn--selected");
		selectedRequirement = req;

		if (tileTypeLabel != null)
			tileTypeLabel.text = string.IsNullOrEmpty(req.tileName) ? req.tileCategory.ToString() : req.tileName;

		if (previewIcon != null)
			previewIcon.style.backgroundImage = req.menuIcon != null ? new StyleBackground(req.menuIcon) : StyleKeyword.None;

		GameManager.Instance.BuildingManager.SetSelectedTile(req.tile);
		RebuildCostRow();
	}

	private void RebuildCostRow()
	{
		costRow.Clear();
		costLabels.Clear();

		if (selectedRequirement == null || selectedRequirement.resourceRequirements == null) return;

		foreach (var r in selectedRequirement.resourceRequirements)
		{
			if (r.resource == null) continue;
			var lbl = new Label($"{r.resource.name} x{r.amount}");
			lbl.AddToClassList("build-cost-label");
			costRow.Add(lbl);
			costLabels.Add(new CostEntry { label = lbl, req = r });
		}
	}

	public void ToggleBuildingMenu()
	{
		if (!isOpen)
		{
			if (!IsHoldingHammer()) return;
			overlay.style.display = DisplayStyle.Flex;
			isOpen = true;
			if (selectedBtn == null && buttons.Count > 0)
				SelectButton(buttons[0].btn, GameManager.Instance.BuildingManager.GetRequirement(buttons[0].tile));
		}
		else
		{
			CloseBuildingMenu();
		}
	}

	public void CloseBuildingMenu()
	{
		if (overlay != null)
			overlay.style.display = DisplayStyle.None;
		isOpen = false;
	}

	private bool IsHoldingHammer()
	{
		var selectedItem = GameManager.Instance.InventoryManager.GetSelectedItem();
		return selectedItem != null && selectedItem == hammerItem;
	}
}
