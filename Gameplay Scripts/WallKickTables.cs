using UnityEngine;

public static class WallKickTables 
{
    public static readonly Vector3Int[][] WallKicksI = new Vector3Int[][]
    {
        new Vector3Int[] { new Vector3Int(0, 0), new Vector3Int(-2, 0), new Vector3Int( 1, 0), new Vector3Int(-2,-1), new Vector3Int( 1, 2) }, //O->R
        new Vector3Int[] { new Vector3Int(0, 0), new Vector3Int( 2, 0), new Vector3Int(-1, 0), new Vector3Int( 2, 1), new Vector3Int(-1,-2) }, //R->0
        new Vector3Int[] { new Vector3Int(0, 0), new Vector3Int(-1, 0), new Vector3Int( 2, 0), new Vector3Int(-1, 2), new Vector3Int( 2,-1) }, //R->2
        new Vector3Int[] { new Vector3Int(0, 0), new Vector3Int( 1, 0), new Vector3Int(-2, 0), new Vector3Int( 1,-2), new Vector3Int(-2, 1) }, //2->R
        new Vector3Int[] { new Vector3Int(0, 0), new Vector3Int( 2, 0), new Vector3Int(-1, 0), new Vector3Int( 2, 1), new Vector3Int(-1,-2) }, //2->L
        new Vector3Int[] { new Vector3Int(0, 0), new Vector3Int(-2, 0), new Vector3Int( 1, 0), new Vector3Int(-2,-1), new Vector3Int( 1, 2) }, //L->2
        new Vector3Int[] { new Vector3Int(0, 0), new Vector3Int( 1, 0), new Vector3Int(-2, 0), new Vector3Int( 1,-2), new Vector3Int(-2, 1) }, //L->0
        new Vector3Int[] { new Vector3Int(0, 0), new Vector3Int(-1, 0), new Vector3Int( 2, 0), new Vector3Int(-1, 2), new Vector3Int( 2,-1) }, //0->L
    };

    public static readonly Vector3Int[][] WallKicksJLOSTZ = new Vector3Int[][] 
    {
        new Vector3Int[] { new Vector3Int(0, 0), new Vector3Int(-1, 0), new Vector3Int(-1, 1), new Vector3Int(0,-2), new Vector3Int(-1,-2) }, //O->R
        new Vector3Int[] { new Vector3Int(0, 0), new Vector3Int( 1, 0), new Vector3Int( 1,-1), new Vector3Int(0, 2), new Vector3Int( 1, 2) }, //R->0
        new Vector3Int[] { new Vector3Int(0, 0), new Vector3Int( 1, 0), new Vector3Int( 1,-1), new Vector3Int(0, 2), new Vector3Int( 1, 2) }, //R->2
        new Vector3Int[] { new Vector3Int(0, 0), new Vector3Int(-1, 0), new Vector3Int(-1, 1), new Vector3Int(0,-2), new Vector3Int(-1,-2) }, //2->R
        new Vector3Int[] { new Vector3Int(0, 0), new Vector3Int( 1, 0), new Vector3Int( 1, 1), new Vector3Int(0,-2), new Vector3Int( 1,-2) }, //2->L
        new Vector3Int[] { new Vector3Int(0, 0), new Vector3Int(-1, 0), new Vector3Int(-1,-1), new Vector3Int(0, 2), new Vector3Int(-1, 2) }, //L->2
        new Vector3Int[] { new Vector3Int(0, 0), new Vector3Int(-1, 0), new Vector3Int(-1,-1), new Vector3Int(0, 2), new Vector3Int(-1, 2) }, //L->0
        new Vector3Int[] { new Vector3Int(0, 0), new Vector3Int( 1, 0), new Vector3Int( 1, 1), new Vector3Int(0,-2), new Vector3Int( 1,-2) }, //0->L
    };
}
