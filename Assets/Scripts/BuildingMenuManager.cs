using UnityEngine;

public class BuildingMenuManager : MonoBehaviour
{
	[SerializeField] private GameObject buildingMenuUI;
	[SerializeField] private Item hammerItem;

	public bool IsBuildingMenuOpen => buildingMenuUI.activeSelf;

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

	public void ToggleBuildingMenu()
	{
		if (!buildingMenuUI.activeSelf)
		{
			if (IsHoldingHammer())
			{
				buildingMenuUI.SetActive(true);
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
