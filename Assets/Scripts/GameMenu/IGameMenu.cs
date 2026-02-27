public interface IGameMenu
{
	string Key { get; }
	bool IsOpen { get; }
	bool IsOverlay { get; }

	void Open();
	void Close();
}
