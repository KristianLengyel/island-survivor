using System.Linq;
using UnityEngine;

public class PlayerToolController : MonoBehaviour
{
	private IPlayerTool[] tools;
	private IPlayerTool activeTool;
	private Item lastItem;

	public void Initialize()
	{
		RefreshTools();
	}

	public void RefreshTools()
	{
		tools = GetComponents<MonoBehaviour>().OfType<IPlayerTool>().ToArray();
	}

	public void Tick()
	{
		var inv = GameManager.Instance.InventoryManager;
		Item selected = inv ? inv.GetSelectedItem() : null;

		if (selected != lastItem)
		{
			SwitchTool(selected);
			lastItem = selected;
		}

		activeTool?.Tick();
	}

	public void FixedTick()
	{
		activeTool?.FixedTick();
	}

	private void SwitchTool(Item newItem)
	{
		activeTool?.OnDeselected();
		activeTool = null;

		if (newItem == null) return;

		for (int i = 0; i < tools.Length; i++)
		{
			if (tools[i].CanHandle(newItem))
			{
				activeTool = tools[i];
				activeTool.OnSelected(newItem);
				return;
			}
		}
	}
}
