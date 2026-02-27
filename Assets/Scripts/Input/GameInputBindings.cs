using UnityEngine;

[CreateAssetMenu(menuName = "Input/Game Input Bindings", fileName = "GameInputBindings")]
public class GameInputBindings : ScriptableObject
{
	[Header("UI")]
	public KeyCode inventoryToggle = KeyCode.Tab;
	public KeyCode buildMenuToggle = KeyCode.T;
	public KeyCode cancel = KeyCode.Escape;

	[Header("Gameplay")]
	public KeyCode interact = KeyCode.E;

	[Header("Map")]
	public KeyCode mapToggle = KeyCode.M;

	[Header("Save/Load")]
	public KeyCode quickSave = KeyCode.F5;
	public KeyCode quickLoad = KeyCode.F9;

	[Header("Debug")]
	public KeyCode adminConsoleToggle = KeyCode.BackQuote;

	[Header("Weather Debug")]
	public KeyCode toggleRain = KeyCode.R;
	public KeyCode toggleFog = KeyCode.F;

	[Header("Carry Debug")]
	public KeyCode pickup = KeyCode.P;
	public KeyCode putdown = KeyCode.O;

	[Header("Toolbar")]
	public KeyCode slot1 = KeyCode.Alpha1;
	public KeyCode slot2 = KeyCode.Alpha2;
	public KeyCode slot3 = KeyCode.Alpha3;
	public KeyCode slot4 = KeyCode.Alpha4;
	public KeyCode slot5 = KeyCode.Alpha5;
	public KeyCode slot6 = KeyCode.Alpha6;

	[Header("Modifiers")]
	public KeyCode leftShift = KeyCode.LeftShift;
	public KeyCode rightShift = KeyCode.RightShift;
	public KeyCode leftCtrl = KeyCode.LeftControl;
	public KeyCode rightCtrl = KeyCode.RightControl;

	[Header("Inventory Debug")]
	public KeyCode stackIncrease = KeyCode.PageUp;
	public KeyCode stackDecrease = KeyCode.PageDown;

	[Header("Axes (Legacy)")]
	public string horizontalAxis = "Horizontal";
	public string verticalAxis = "Vertical";
	public string scrollAxis = "Mouse ScrollWheel";
}
