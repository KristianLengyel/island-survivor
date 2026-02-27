using System.IO;
using UnityEngine;

public class LowResScreenshot : MonoBehaviour
{
	public RenderTexture screenshotRenderTexture; // Assign your 320x180 RenderTexture in the Inspector
	public KeyCode screenshotKey = KeyCode.F12;   // Press this key to capture
	public string folderPath = "Screenshots";     // Subfolder in Application.persistentDataPath

	private RenderTexture originalTargetTexture;
	private int originalCullingMask;

	void Update()
	{
		if (Input.GetKeyDown(screenshotKey))
		{
			CaptureScreenshot();
		}
	}

	public void CaptureScreenshot()
	{
		// Backup original camera settings
		originalTargetTexture = Camera.main.targetTexture;
		originalCullingMask = Camera.main.cullingMask;

		// Setup directory
		string fullPath = Path.Combine(Application.persistentDataPath, folderPath);
		if (!Directory.Exists(fullPath)) Directory.CreateDirectory(fullPath);

		// Temporarily render to the low-res RenderTexture
		Camera.main.targetTexture = screenshotRenderTexture;
		Camera.main.cullingMask = ~0; // Render everything (adjust if you have layers to exclude, e.g., UI)

		// Force a render
		Camera.main.Render();

		// Read pixels from the RenderTexture
		RenderTexture.active = screenshotRenderTexture;
		Texture2D tex = new Texture2D(screenshotRenderTexture.width, screenshotRenderTexture.height, TextureFormat.RGB24, false);
		tex.ReadPixels(new Rect(0, 0, screenshotRenderTexture.width, screenshotRenderTexture.height), 0, 0);
		tex.Apply();

		// Save to file
		byte[] bytes = tex.EncodeToPNG();
		string filename = Path.Combine(fullPath, "Screenshot_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".png");
		File.WriteAllBytes(filename, bytes);
		Debug.Log("Low-res screenshot saved: " + filename);

		// Cleanup
		Destroy(tex);
		RenderTexture.active = null;

		// Restore original camera settings
		Camera.main.targetTexture = originalTargetTexture;
		Camera.main.cullingMask = originalCullingMask;
	}
}