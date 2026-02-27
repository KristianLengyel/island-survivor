using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ExportProjectKnowledge
{
	[MenuItem("Tools/AI/Export Project Knowledge (JSON)")]
	public static void Export()
	{
		if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
			return;

		var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
		var outPath = Path.Combine(projectRoot, "ProjectKnowledge.json");

		var export = new Dictionary<string, object>
		{
			["generatedAtUtc"] = DateTime.UtcNow.ToString("o"),
			["unityVersion"] = Application.unityVersion,
			["scriptableObjects"] = ExportAllScriptableObjects(),
			["prefabs"] = ExportAllPrefabs(),
			["scenes"] = ExportAllScenesInAssetsOnly()
		};

		var json = JsonWriter.ToJson(export, pretty: true);
		File.WriteAllText(outPath, json, Encoding.UTF8);

		AssetDatabase.Refresh();
		Debug.Log($"Exported ProjectKnowledge.json to: {outPath}");
	}

	private static List<Dictionary<string, object>> ExportAllScriptableObjects()
	{
		var results = new List<Dictionary<string, object>>();
		var guids = AssetDatabase.FindAssets("t:ScriptableObject");

		foreach (var guid in guids)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			if (string.IsNullOrWhiteSpace(path)) continue;

			var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
			if (obj == null) continue;

			results.Add(new Dictionary<string, object>
			{
				["guid"] = guid,
				["path"] = path,
				["type"] = obj.GetType().FullName,
				["data"] = EditorJsonUtility.ToJson(obj, true)
			});
		}

		return results;
	}

	private static List<Dictionary<string, object>> ExportAllPrefabs()
	{
		var results = new List<Dictionary<string, object>>();
		var guids = AssetDatabase.FindAssets("t:Prefab");

		foreach (var guid in guids)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			if (string.IsNullOrWhiteSpace(path)) continue;

			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			if (prefab == null) continue;

			var components = new List<Dictionary<string, object>>();
			var monos = prefab.GetComponentsInChildren<MonoBehaviour>(true);

			foreach (var mb in monos)
			{
				if (mb == null) continue;

				var c = new Dictionary<string, object>
				{
					["gameObject"] = mb.gameObject.name,
					["componentType"] = mb.GetType().FullName
				};

				var script = MonoScript.FromMonoBehaviour(mb);
				if (script != null)
				{
					var scriptPath = AssetDatabase.GetAssetPath(script);
					c["scriptPath"] = scriptPath;
					c["scriptGuid"] = AssetDatabase.AssetPathToGUID(scriptPath);
				}

				components.Add(c);
			}

			results.Add(new Dictionary<string, object>
			{
				["guid"] = guid,
				["path"] = path,
				["components"] = components
			});
		}

		return results;
	}

	private static List<Dictionary<string, object>> ExportAllScenesInAssetsOnly()
	{
		var results = new List<Dictionary<string, object>>();

		var originalScene = SceneManager.GetActiveScene();
		var originalPath = originalScene.IsValid() ? originalScene.path : null;

		var sceneGuids = AssetDatabase.FindAssets("t:Scene");
		foreach (var guid in sceneGuids)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			if (string.IsNullOrWhiteSpace(path)) continue;

			if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
				continue;

			Scene opened;
			try
			{
				opened = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
			}
			catch (Exception e)
			{
				Debug.LogWarning($"Skipping scene '{path}' (failed to open): {e.Message}");
				continue;
			}

			var roots = new List<Dictionary<string, object>>();
			foreach (var root in opened.GetRootGameObjects())
			{
				var components = new List<Dictionary<string, object>>();
				var monos = root.GetComponentsInChildren<MonoBehaviour>(true);

				foreach (var mb in monos)
				{
					if (mb == null) continue;

					var c = new Dictionary<string, object>
					{
						["gameObject"] = mb.gameObject.name,
						["componentType"] = mb.GetType().FullName
					};

					var script = MonoScript.FromMonoBehaviour(mb);
					if (script != null)
					{
						var scriptPath = AssetDatabase.GetAssetPath(script);
						c["scriptPath"] = scriptPath;
						c["scriptGuid"] = AssetDatabase.AssetPathToGUID(scriptPath);
					}

					components.Add(c);
				}

				roots.Add(new Dictionary<string, object>
				{
					["name"] = root.name,
					["components"] = components
				});
			}

			results.Add(new Dictionary<string, object>
			{
				["guid"] = guid,
				["path"] = path,
				["rootObjects"] = roots
			});

			if (SceneManager.sceneCount > 1)
				EditorSceneManager.CloseScene(opened, true);
		}

		if (!string.IsNullOrWhiteSpace(originalPath) && originalPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
		{
			try { EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single); }
			catch { }
		}

		return results;
	}

	private static class JsonWriter
	{
		public static string ToJson(object obj, bool pretty)
		{
			var sb = new StringBuilder(1024);
			WriteValue(sb, obj, pretty, 0);
			return sb.ToString();
		}

		private static void WriteValue(StringBuilder sb, object value, bool pretty, int indent)
		{
			if (value == null)
			{
				sb.Append("null");
				return;
			}

			if (value is string s)
			{
				sb.Append('"');
				sb.Append(Escape(s));
				sb.Append('"');
				return;
			}

			if (value is bool b)
			{
				sb.Append(b ? "true" : "false");
				return;
			}

			if (IsNumber(value))
			{
				sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
				return;
			}

			if (value is IDictionary dict)
			{
				WriteObject(sb, dict, pretty, indent);
				return;
			}

			if (value is IEnumerable enumerable && !(value is string))
			{
				WriteArray(sb, enumerable, pretty, indent);
				return;
			}

			sb.Append('"');
			sb.Append(Escape(value.ToString()));
			sb.Append('"');
		}

		private static void WriteObject(StringBuilder sb, IDictionary dict, bool pretty, int indent)
		{
			sb.Append('{');
			var first = true;

			foreach (DictionaryEntry de in dict)
			{
				if (de.Key == null) continue;

				if (!first) sb.Append(',');
				first = false;

				if (pretty) NewLineAndIndent(sb, indent + 1);

				sb.Append('"');
				sb.Append(Escape(de.Key.ToString()));
				sb.Append('"');
				sb.Append(pretty ? ": " : ":");

				WriteValue(sb, de.Value, pretty, indent + 1);
			}

			if (pretty && dict.Count > 0) NewLineAndIndent(sb, indent);
			sb.Append('}');
		}

		private static void WriteArray(StringBuilder sb, IEnumerable arr, bool pretty, int indent)
		{
			sb.Append('[');
			var first = true;

			foreach (var item in arr)
			{
				if (!first) sb.Append(',');
				first = false;

				if (pretty) NewLineAndIndent(sb, indent + 1);
				WriteValue(sb, item, pretty, indent + 1);
			}

			if (pretty)
			{
				var any = false;
				foreach (var _ in arr) { any = true; break; }
				if (any) NewLineAndIndent(sb, indent);
			}

			sb.Append(']');
		}

		private static void NewLineAndIndent(StringBuilder sb, int indent)
		{
			sb.Append('\n');
			for (var i = 0; i < indent; i++) sb.Append("  ");
		}

		private static string Escape(string s)
		{
			return s
				.Replace("\\", "\\\\")
				.Replace("\"", "\\\"")
				.Replace("\n", "\\n")
				.Replace("\r", "\\r")
				.Replace("\t", "\\t");
		}

		private static bool IsNumber(object v)
		{
			return v is sbyte || v is byte || v is short || v is ushort ||
				   v is int || v is uint || v is long || v is ulong ||
				   v is float || v is double || v is decimal;
		}
	}
}
