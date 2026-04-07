using UnityEngine;
using UnityEngine.Tilemaps;

public class PipeRuleTile : RuleTile
{
	public override bool RuleMatches(TilingRule rule, Vector3Int position, ITilemap tilemap, ref Matrix4x4 transform)
	{
		for (int i = 0; i < rule.m_Neighbors.Count && i < rule.m_NeighborPositions.Count; i++)
		{
			int neighbor = rule.m_Neighbors[i];
			Vector3Int nPos = position + rule.m_NeighborPositions[i];
			TileBase nTile = tilemap.GetTile(nPos);

			bool connects = nTile == this || IsStationAt(nPos);

			switch (neighbor)
			{
				case TilingRule.Neighbor.This:
					if (!connects) return false;
					break;
				case TilingRule.Neighbor.NotThis:
					if (connects) return false;
					break;
			}
		}
		transform = Matrix4x4.identity;
		return true;
	}

	private bool IsStationAt(Vector3Int pos)
	{
		return PipeNetwork.Instance != null &&
			   PipeNetwork.Instance.HasStationAt(new Vector2Int(pos.x, pos.y));
	}
}
