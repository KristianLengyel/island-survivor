using System.IO;
using UnityEngine;

public static class HeightmapIO
{
	public static void SaveFloatArray(string directoryName, string fileName, float[,] data)
	{
		string directoryPath = Path.Combine(Application.persistentDataPath, directoryName);
		if (!Directory.Exists(directoryPath))
			Directory.CreateDirectory(directoryPath);

		string filePath = Path.Combine(directoryPath, fileName);
		using (StreamWriter writer = new StreamWriter(filePath))
		{
			int width = data.GetLength(0);
			int height = data.GetLength(1);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					writer.Write(data[x, y].ToString("F3") + " ");
				}
				writer.WriteLine();
			}
		}
	}

	public static void SaveHeightmapTexture(string directoryName, string fileName, float[,] data)
	{
		int width = data.GetLength(0);
		int height = data.GetLength(1);

		Texture2D texture = new Texture2D(width, height);
		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{
				float value = data[x, y];
				texture.SetPixel(x, y, new Color(value, value, value));
			}
		}
		texture.Apply();

		string directoryPath = Path.Combine(Application.persistentDataPath, directoryName);
		if (!Directory.Exists(directoryPath))
			Directory.CreateDirectory(directoryPath);

		string filePath = Path.Combine(directoryPath, fileName + ".png");
		File.WriteAllBytes(filePath, texture.EncodeToPNG());
	}
}