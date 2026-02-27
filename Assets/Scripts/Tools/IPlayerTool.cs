public interface IPlayerTool
{
	bool CanHandle(Item selectedItem);
	void OnSelected(Item selectedItem);
	void OnDeselected();
	void Tick();
	void FixedTick();
}
