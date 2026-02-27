using UnityEngine;
using UnityEngine.UIElements;

public class PixelPerfectPanelScaler : MonoBehaviour
{
	[SerializeField] private PanelSettings panelSettings;
	[SerializeField] private Vector2Int referenceResolution = new Vector2Int(320, 180);
	[SerializeField] private bool clampToAtLeast1x = true;

	int lastW;
	int lastH;

	void OnEnable() => Apply();

	void Update()
	{
		if (Screen.width != lastW || Screen.height != lastH)
			Apply();
	}

	void Apply()
	{
		lastW = Screen.width;
		lastH = Screen.height;

		int scaleX = Mathf.FloorToInt((float)Screen.width / referenceResolution.x);
		int scaleY = Mathf.FloorToInt((float)Screen.height / referenceResolution.y);
		int scale = Mathf.Min(scaleX, scaleY);

		if (clampToAtLeast1x)
			scale = Mathf.Max(1, scale);

		panelSettings.scale = scale;
	}
}
