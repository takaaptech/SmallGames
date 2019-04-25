using UnityEngine;
public class TileInfos {
    public Vector2Int min;
    public Vector2Int size;
    public ushort[] tiles;
}

public class GridInfo {
    public Vector3 cellSize;
    public Vector3 cellGap;
    public int cellLayout;
    public int cellSwizzle;
    public TileInfos[] tileMaps;
    public string[] names;
}
