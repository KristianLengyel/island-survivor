using UnityEngine;

public class NoiseGenerator
{
	public struct NoiseMaps
	{
		public float[,] heightmap;
		public float[,] seaweedMap;
	}

	public NoiseMaps GenerateHeightmap(
		int mapSize, float scale, float offsetX, float offsetY,
		int numOctaves, float persistence, float lacunarity,
		float seaweedScale, float seaweedOffsetX, float seaweedOffsetY)
	{
		float[,] heightmap = new float[mapSize, mapSize];
		float[,] seaweedMap = new float[mapSize, mapSize];

		for (int x = 0; x < mapSize; x++)
		{
			for (int y = 0; y < mapSize; y++)
			{
				float amplitude = 1f;
				float frequency = 1f;
				float noiseHeight = 0f;
				for (int i = 0; i < numOctaves; i++)
				{
					float sampleX = (x / scale * frequency) + offsetX;
					float sampleY = (y / scale * frequency) + offsetY;
					float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f;
					noiseHeight += perlinValue * amplitude;
					amplitude *= persistence;
					frequency *= lacunarity;
				}
				heightmap[x, y] = Mathf.InverseLerp(-1f, 1f, noiseHeight);

				float seaweedX = (float)x / mapSize * seaweedScale + seaweedOffsetX;
				float seaweedY = (float)y / mapSize * seaweedScale + seaweedOffsetY;
				seaweedMap[x, y] = Mathf.PerlinNoise(seaweedX, seaweedY);
			}
		}

		return new NoiseMaps { heightmap = heightmap, seaweedMap = seaweedMap };
	}
}