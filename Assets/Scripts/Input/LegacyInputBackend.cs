using UnityEngine;

public sealed class LegacyInputBackend : IInputBackend
{
	public Vector2 Move { get; private set; }
	public Vector2 MouseScreen { get; private set; }
	public float Scroll { get; private set; }

	public bool ShiftHeld { get; private set; }
	public bool CtrlHeld { get; private set; }

	public bool InteractDown { get; private set; }
	public bool InventoryDown { get; private set; }
	public bool BuildMenuDown { get; private set; }
	public bool CancelDown { get; private set; }

	public bool MapDown { get; private set; }
	public bool RevealMapDown { get; private set; }

	public bool QuickSaveDown { get; private set; }
	public bool QuickLoadDown { get; private set; }

	public bool ToggleRainDown { get; private set; }
	public bool ToggleFogDown { get; private set; }

	public bool PickupDown { get; private set; }
	public bool PutdownDown { get; private set; }

	public int ToolbarSlotDown { get; private set; }

	public bool LmbDown { get; private set; }
	public bool LmbHeld { get; private set; }
	public bool LmbUp { get; private set; }

	public bool RmbDown { get; private set; }
	public bool RmbHeld { get; private set; }
	public bool RmbUp { get; private set; }

	public bool StackIncreaseDown { get; private set; }
	public bool StackDecreaseDown { get; private set; }

	public bool AdminConsoleDown { get; private set; }

	public void Tick(GameInputBindings b)
	{
		Move = new Vector2(
			Input.GetAxisRaw(b.horizontalAxis),
			Input.GetAxisRaw(b.verticalAxis)
		);

		MouseScreen = Input.mousePosition;
		Scroll = Input.GetAxisRaw(b.scrollAxis);

		ShiftHeld = Input.GetKey(b.leftShift) || Input.GetKey(b.rightShift);
		CtrlHeld = Input.GetKey(b.leftCtrl) || Input.GetKey(b.rightCtrl);

		InteractDown = Input.GetKeyDown(b.interact);
		InventoryDown = Input.GetKeyDown(b.inventoryToggle);
		BuildMenuDown = Input.GetKeyDown(b.buildMenuToggle);
		CancelDown = Input.GetKeyDown(b.cancel);

		MapDown = Input.GetKeyDown(b.mapToggle);
		RevealMapDown = CtrlHeld && Input.GetKeyDown(b.mapToggle);

		QuickSaveDown = Input.GetKeyDown(b.quickSave);
		QuickLoadDown = Input.GetKeyDown(b.quickLoad);

		ToggleRainDown = Input.GetKeyDown(b.toggleRain);
		ToggleFogDown = Input.GetKeyDown(b.toggleFog);

		PickupDown = Input.GetKeyDown(b.pickup);
		PutdownDown = Input.GetKeyDown(b.putdown);

		StackIncreaseDown = Input.GetKeyDown(b.stackIncrease);
		StackDecreaseDown = Input.GetKeyDown(b.stackDecrease);

		ToolbarSlotDown =
			Input.GetKeyDown(b.slot1) ? 0 :
			Input.GetKeyDown(b.slot2) ? 1 :
			Input.GetKeyDown(b.slot3) ? 2 :
			Input.GetKeyDown(b.slot4) ? 3 :
			Input.GetKeyDown(b.slot5) ? 4 :
			Input.GetKeyDown(b.slot6) ? 5 : -1;

		LmbDown = Input.GetMouseButtonDown(0);
		LmbHeld = Input.GetMouseButton(0);
		LmbUp = Input.GetMouseButtonUp(0);

		RmbDown = Input.GetMouseButtonDown(1);
		RmbHeld = Input.GetMouseButton(1);
		RmbUp = Input.GetMouseButtonUp(1);

		AdminConsoleDown =
			Input.GetKeyDown(b.adminConsoleToggle)
			|| Input.GetKeyDown(KeyCode.BackQuote)
			|| Input.GetKeyDown(KeyCode.Semicolon);
	}
}
