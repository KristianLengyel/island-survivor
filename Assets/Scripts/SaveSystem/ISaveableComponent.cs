public interface ISaveableComponent
{
	string SaveKey { get; }
	string CaptureStateJson();
	void RestoreStateJson(string json);
}
