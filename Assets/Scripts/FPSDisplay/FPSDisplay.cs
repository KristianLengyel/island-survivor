using TMPro;
using UnityEngine;

public class FPSDisplay : MonoBehaviour
{
	public TMP_Text fpsText;
	private float deltaTime;
	private float updateInterval = 0.2f;
	private float timeSinceLastUpdate = 0f;

	void Update()
	{
		deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
		timeSinceLastUpdate += Time.unscaledDeltaTime;

		if (timeSinceLastUpdate >= updateInterval)
		{
			float fps = 1.0f / deltaTime;
			fpsText.text = string.Format("{0:0.} fps", fps);
			timeSinceLastUpdate = 0f;
		}
	}
}
