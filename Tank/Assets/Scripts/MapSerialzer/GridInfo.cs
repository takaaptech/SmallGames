using UnityEngine;
using UnityEngine.Tilemaps;

public class TileInfos {
    public Vector2Int min;
    public Vector2Int size;
    public ushort[] tileIDs;

    public TileBase GetTile(Vector2Int pos){
        var diff = pos - min;
        if (diff.x > size.x || diff.y > size.y) {
            return null;
        }
        var id = tileIDs[diff.y * size.y + diff.x];
        return LevelManager.ID2Tile(id);
    }
    public TileBase[] GetAllTiles(){
        var count = tileIDs.Length;
        var tiles = new TileBase[count];
        for (int i = 0; i < count; i++) {
            tiles[i] = LevelManager.ID2Tile(tileIDs[i]);
        }
        return tiles;
    }

    public Vector3Int[] GetAllPositions(){
        var poss = new Vector3Int[tileIDs.Length];
        var sx = min.x;
        var sy = min.y;
        var sizex = size.x;
        var sizey = size.y;
        for (int y = 0; y < sizey; y++) {
            for (int x = 0; x < sizex; x++) {
                poss[y*sizex + x] = new Vector3Int(sx + x,sy + y,0);
            }
        }
        return poss;
    }
    
}

public class GridInfo {
    public Vector3 cellSize;
    public Vector3 cellGap;
    public int cellLayout;
    public int cellSwizzle;
    public TileInfos[] tileMaps;
    public string[] names;

    public TileInfos GetMapInfo(string name){
        if (names == null) return null;
        for (int i = 0; i < names.Length; i++) {
            if (names[i] == name) {
                return tileMaps[i];
            }
        }

        return null;
    }
}