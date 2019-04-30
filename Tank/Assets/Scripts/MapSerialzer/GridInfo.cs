using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class TileInfos {
    public Vector2Int min;
    public Vector2Int size;
    public ushort[] tileIDs;
    public TileBase[] allTiles;
    public Vector3Int[] allPos;
    public bool hasCollider = true;

    /// <summary> 仅用于标记 </summary>
    public bool isTagMap = false;

    public Tilemap tilemap;

    public TileBase GetTile(Vector2Int pos){
        var diff = pos - min;
        if (diff.x < 0 || diff.y < 0 || diff.x >= size.x || diff.y >= size.y) {
            return null;
        }

        var id = tileIDs[diff.y * size.x + diff.x];
        return LevelManager.ID2Tile(id);
    }

    public ushort GetTileID(Vector2Int pos){
        var diff = pos - min;
        if (diff.x < 0 || diff.y < 0 || diff.x >= size.x || diff.y >= size.y) {
            return 0;
        }

        var id = tileIDs[diff.y * size.x + diff.x];
        return id;
    }

    public void SetTileID(Vector2Int pos, ushort id){
        //TODO 兼容地图扩大
        var diff = pos - min;
        if (diff.x < 0 || diff.y < 0 || diff.x >= size.x || diff.y >= size.y) {
            return;
        }

        var idx = diff.y * size.x + diff.x;
        tileIDs[idx] = id;
        var tile = LevelManager.ID2Tile(id);
        if (allTiles != null) {
            allTiles[idx] = tile;
        }

        tilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), tile);
    }

    public List<Vector2Int> GetAllTiles(TileBase type){
        var lst = new List<Vector2Int>();
        var tiles = GetAllTiles();
        var poss = GetAllPositions();
        var count = tiles.Length;
        for (int i = 0; i < count; i++) {
            if (tiles[i] == type) {
                lst.Add(new Vector2Int(poss[i].x, poss[i].y));
            }
        }

        return lst;
    }

    public TileBase[] GetAllTiles(){
        if (allTiles != null)
            return allTiles;
        var count = tileIDs.Length;
        var tiles = new TileBase[count];
        for (int i = 0; i < count; i++) {
            tiles[i] = LevelManager.ID2Tile(tileIDs[i]);
        }

        allTiles = tiles;
        return tiles;
    }

    public Vector3Int[] GetAllPositions(){
        if (allPos != null)
            return allPos;
        var poss = new Vector3Int[tileIDs.Length];
        var sx = min.x;
        var sy = min.y;
        var sizex = size.x;
        var sizey = size.y;
        for (int y = 0; y < sizey; y++) {
            for (int x = 0; x < sizex; x++) {
                poss[y * sizex + x] = new Vector3Int(sx + x, sy + y, 0);
            }
        }

        allPos = poss;
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