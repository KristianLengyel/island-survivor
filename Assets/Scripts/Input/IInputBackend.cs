using UnityEngine;

public interface IInputBackend
{
	void Tick(GameInputBindings bindings);

	Vector2 Move { get; }
	Vector2 MouseScreen { get; }
	float Scroll { get; }

	bool ShiftHeld { get; }
	bool CtrlHeld { get; }

	bool InteractDown { get; }
	bool InventoryDown { get; }
	bool BuildMenuDown { get; }
	bool CancelDown { get; }

	bool MapDown { get; }
	bool RevealMapDown { get; }

	bool QuickSaveDown { get; }
	bool QuickLoadDown { get; }

	bool ToggleRainDown { get; }
	bool ToggleFogDown { get; }

	bool PickupDown { get; }
	bool PutdownDown { get; }

	int ToolbarSlotDown { get; }

	bool LmbDown { get; }
	bool LmbHeld { get; }
	bool LmbUp { get; }

	bool RmbDown { get; }
	bool RmbHeld { get; }
	bool RmbUp { get; }

	bool StackIncreaseDown { get; }
	bool StackDecreaseDown { get; }

	bool AdminConsoleDown { get; }
}
