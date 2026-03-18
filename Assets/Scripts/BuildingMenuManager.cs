using System.Collections.Generic;
using UnityEngine;

public class BuildingMenuManager : MonoBehaviour
{
	[SerializeField] private GameObject buildingMenuUI;
	[SerializeField] private Item hammerItem;
	[SerializeField] private BuildingTileButton buttonPrefab;
	[SerializeField] private Transform buttonContainer;
	[SerializeField] private List<TileResourceRequirement> tileRequirements;

	private BuildingTileButton selectedButton;
	private readonly List<BuildingTileButton> buttons = new List<BuildingTileButton>();

	public bool IsBuildingMenuOpen => buildingMenuUI.activeSelf;

	private void Start()
	{
		BuildButtons();
	}

	private void Update()
	{
		if (GameInput.BuildMenuDown)
		{
			MenuCoordinator.Instance.Toggle("BuildMenu");
		}
		else if (GameInput.CancelDown && buildingMenuUI.activeSelf)
		{
			CloseBuildingMenu();
		}
	}

	private void BuildButtons()
	{
		if (buttonPrefab == null || buttonContainer == null) return;

		foreach (var req in tileRequirements)
		{
			if (req == null || req.tile == null) continue;

			BuildingTileButton btn = Instantiate(buttonPrefab, buttonContainer);
			btn.Initialize(req.tile, req.menuIcon, this);
			buttons.Add(btn);
		}
	}

	private void ApplySelection(BuildingTileButton button)
	{
		if (selectedButton != null)
			selectedButton.SetSelected(false);
		selectedButton = button;
		selectedButton.SetSelected(true);
		GameManager.Instance.BuildingManager.SetSelectedTile(button.GetTile());
	}

	public void SelectButton(BuildingTileButton button)
	{
		if (selectedButton == button) return;
		ApplySelection(button);
	}

	public void ToggleBuildingMenu()
	{
		if (!buildingMenuUI.activeSelf)
		{
			if (IsHoldingHammer())
			{
				buildingMenuUI.SetActive(true);
				if (selectedButton == null && buttons.Count > 0)
					ApplySelection(buttons[0]);
				else if (selectedButton != null)
					selectedButton.SetSelected(true);
			}
		}
		else
		{
			buildingMenuUI.SetActive(false);
		}
	}

	public void CloseBuildingMenu()
	{
		buildingMenuUI.SetActive(false);
	}

	private bool IsHoldingHammer()
	{
		var selectedItem = GameManager.Instance.InventoryManager.GetSelectedItem();
		return selectedItem != null && selectedItem == hammerItem;
	}
}
