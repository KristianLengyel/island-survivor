using System.Collections.Generic;
using UnityEngine;

public class PlayerTileDetector : MonoBehaviour
{
	public Transform playerSpriteMask;
	public Transform playerTileChecker;

	public float landOffset = 0f;
	public float shallowWaterOffset = 0.35f;
	public float mediumWaterOffset = 0.55f;
	public float deepWaterOffset = 0.75f;
	public float abyssWaterOffset = 0.75f;

	private TileChecker tileChecker;
	private Vector3 originalMaskPosition;
	private Vector3 lastPosition;
	private string lastTileType = "None";
	private bool isInWater;
	private Dictionary<string, float> tileOffsets;

	private void Awake()
	{
		tileChecker = FindAnyObjectByType<TileChecker>();
		if (playerSpriteMask != null)
			originalMaskPosition = playerSpriteMask.localPosition;

		tileOffsets = new Dictionary<string, float>
		{
			{ "Land", landOffset },
			{ "None", landOffset },
			{ "Shallow", shallowWaterOffset },
			{ "Medium", mediumWaterOffset },
			{ "Deep", deepWaterOffset },
			{ "Abyss", abyssWaterOffset },
			{ "Water", mediumWaterOffset }
		};

		if (tileChecker == null || playerTileChecker == null || playerSpriteMask == null)
			enabled = false;
	}

	private void Start()
	{
		UpdateMask(playerTileChecker.position);
	}

	private void Update()
	{
		Vector3 currentPosition = playerTileChecker.position;
		if (currentPosition != lastPosition)
		{
			UpdateMask(currentPosition);
			lastPosition = currentPosition;
		}
	}

	private void UpdateMask(Vector3 position)
	{
		string tileType = tileChecker.CheckTileType(position);
		if (tileType != lastTileType)
		{
			float offset = tileOffsets.ContainsKey(tileType) ? tileOffsets[tileType] : 0f;

			if (tileType == "Land" || tileType == "None")
			{
				playerSpriteMask.localPosition = originalMaskPosition;
				isInWater = false;
			}
			else
			{
				Vector3 maskPosition = originalMaskPosition;
				maskPosition.y += offset;
				playerSpriteMask.localPosition = maskPosition;
				isInWater = true;
			}
			lastTileType = tileType;
		}
	}

	public bool IsInWater() => isInWater;

	public bool IsInShallowWater() => isInWater && lastTileType == "Shallow";

	public bool IsTileWater(Vector2 position)
	{
		string tileType = tileChecker.CheckTileType(position);
		return tileType == "Shallow" || tileType == "Medium" || tileType == "Deep" || tileType == "Water";
	}
}
