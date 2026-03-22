using System.Linq;
using UnityEditor;
using UnityEngine;

public static class RuleTileSetupTool
{
    [MenuItem("Tools/Rule Tile/Assign Sprites From Sheet")]
    static void AssignSpritesFromSheet()
    {
        RuleTile tile = null;
        Texture2D texture = null;
        foreach (var obj in Selection.objects)
        {
            if (obj is RuleTile rt) tile = rt;
            else if (obj is Texture2D tex) texture = tex;
        }
        if (tile == null || texture == null)
        {
            EditorUtility.DisplayDialog("Rule Tile Setup", "Select both a RuleTile and a Texture2D sprite sheet in the Project window.", "OK");
            return;
        }
        AssignSprites(tile, texture);
    }

    [MenuItem("Tools/Rule Tile/Assign Sprites From Sheet", true)]
    static bool AssignSpritesValidate()
    {
        return Selection.objects.Any(o => o is RuleTile) && Selection.objects.Any(o => o is Texture2D);
    }

    [MenuItem("Tools/Rule Tile/Create From Template")]
    static void OpenCreateWindow()
    {
        RuleTileCreatorWindow.Open();
    }

    public static void AssignSprites(RuleTile tile, Texture2D texture)
    {
        string texPath = AssetDatabase.GetAssetPath(texture);
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(texPath)
            .OfType<Sprite>()
            .OrderBy(s => ParseSpriteIndex(s.name))
            .ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogError($"No sprites found in {texPath}. Make sure the texture is sliced into multiple sprites.");
            return;
        }

        Undo.RecordObject(tile, "Assign Sprites to Rule Tile");

        for (int i = 0; i < tile.m_TilingRules.Count; i++)
        {
            if (i >= sprites.Length)
            {
                Debug.LogWarning($"{tile.name}: rule {i} has no matching sprite (sheet has {sprites.Length} sprites).");
                break;
            }
            tile.m_TilingRules[i].m_Sprites = new Sprite[] { sprites[i] };
        }

        tile.m_DefaultSprite = sprites[0];

        EditorUtility.SetDirty(tile);
        AssetDatabase.SaveAssets();
        Debug.Log($"Assigned {Mathf.Min(tile.m_TilingRules.Count, sprites.Length)} sprites to {tile.name}.");
    }

    static int ParseSpriteIndex(string name)
    {
        int underscore = name.LastIndexOf('_');
        if (underscore >= 0 && int.TryParse(name.Substring(underscore + 1), out int index))
            return index;
        return int.MaxValue;
    }
}

public class RuleTileCreatorWindow : EditorWindow
{
    RuleTile templateTile;
    Texture2D spriteSheet;
    string tileName = "NewRuleTile";
    string outputFolder = "Assets/Resources/CustomTiles";

    public static void Open()
    {
        GetWindow<RuleTileCreatorWindow>("Create Rule Tile").Show();
    }

    void OnGUI()
    {
        templateTile = (RuleTile)EditorGUILayout.ObjectField("Template Tile", templateTile, typeof(RuleTile), false);
        spriteSheet = (Texture2D)EditorGUILayout.ObjectField("Sprite Sheet", spriteSheet, typeof(Texture2D), false);
        tileName = EditorGUILayout.TextField("Tile Name", tileName);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);

        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(templateTile == null || spriteSheet == null || string.IsNullOrEmpty(tileName));
        if (GUILayout.Button("Create Rule Tile"))
            Create();
        EditorGUI.EndDisabledGroup();
    }

    void Create()
    {
        string templatePath = AssetDatabase.GetAssetPath(templateTile);
        string outputPath = $"{outputFolder}/{tileName}.asset";

        if (!AssetDatabase.CopyAsset(templatePath, outputPath))
        {
            EditorUtility.DisplayDialog("Error", $"Could not copy to {outputPath}. Check the output folder exists.", "OK");
            return;
        }

        AssetDatabase.Refresh();
        RuleTile newTile = AssetDatabase.LoadAssetAtPath<RuleTile>(outputPath);
        if (newTile == null)
        {
            Debug.LogError($"Failed to load new tile at {outputPath}");
            return;
        }

        RuleTileSetupTool.AssignSprites(newTile, spriteSheet);
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = newTile;
    }
}
