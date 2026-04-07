using UnityEngine;
using UnityEngine.Tilemaps;

public class SyncedAnimatedRuleTile : RuleTile
{
    public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
    {
        if (!base.GetTileAnimationData(position, tilemap, ref tileAnimationData))
            return false;
        tileAnimationData.animationStartTime = -Time.time;
        return true;
    }
}
