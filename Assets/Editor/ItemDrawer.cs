using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Item))]
public class ItemDrawer : Editor
{
	public override void OnInspectorGUI()
	{
		Item item = (Item)target;

		// Draw the default Inspector fields
		DrawDefaultInspector();

		// Set the icon for the ScriptableObject in the Project window if an image is assigned
		if (item.image != null)
		{
			// Get the sprite texture (ensure it's a valid texture for the icon)
			Texture2D iconTexture = AssetPreview.GetAssetPreview(item.image);
			if (iconTexture != null)
			{
				// Use EditorGUIUtility.SetIconForObject for Unity 2019+
				EditorGUIUtility.SetIconForObject(target, iconTexture);

				// Optionally, save the changes (though Unity often handles this automatically)
				AssetDatabase.SaveAssets();
			}
			else
			{
				Debug.LogWarning($"Could not generate preview for sprite in {item.name}. Check sprite settings.");
			}
		}
	}
}