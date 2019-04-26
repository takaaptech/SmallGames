using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;

#endif

[System.Serializable]
public class LevelManager : BaseManager {
    private static bool hasLoadIDMapConfig = false; // 是否已经加载了配置
    private static string idMapPath = "TileIDMap";
    private static TileBase[] id2Tiles = new TileBase[65536]; //64KB
    private static Dictionary<TileBase, ushort> tile2ID = new Dictionary<TileBase, ushort>();

    public static int curLevel = 1;


    private static string TILE_MAP_NAME_BORN_POS = "BornPos";

    public static string GetMapPathFull(int level){
        return Path.Combine(Application.dataPath, "Resources/Maps/" + level + ".bytes");
    }
    
    public void OnSceneLoaded(){
        LoadGame();
    }

    public void LoadGame(){
        Main.gameMgr.StartGame();
        LoadLevel(curLevel);
    }

    public static void LoadLevel(int level){
        CheckLoadTileIDMap();
        var path = LevelManager.GetMapPathFull(level);
        if (!File.Exists(path)) {
            Debug.LogError("Have no map file" + level);
            return;
        }
        var bytes = File.ReadAllBytes(path);
        var reader = new BinaryReader(new MemoryStream(bytes));
        var info = TileMapSerializer.ReadGrid(reader);
        var go = GameObject.FindObjectOfType<Grid>();
        if (go != null) {
            var maps = go.GetComponentsInChildren<Tilemap>();
            for (int i = 0; i < maps.Length; i++) {
                var tileMap = maps[i];
                var tileMapInfo = info.GetMapInfo(tileMap.name);
                if(tileMapInfo== null)
                    continue;
                tileMap.ClearAllTiles();
                tileMap.SetTiles(tileMapInfo.GetAllPositions(), tileMapInfo.GetAllTiles());
                if (Application.isPlaying) {
                    if (tileMap.name == TILE_MAP_NAME_BORN_POS) {
                        tileMap.GetComponent<TilemapRenderer>().enabled = false;
                    }
                }
            }
        }
    }

    public static void SaveLevel(int level){
        var go = GameObject.FindObjectOfType<Grid>();
        if (go == null)
            return;
        var grid = go.GetComponent<Grid>();
        if (grid == null) return;
        var bytes = TileMapSerializer.SerializeGrid(grid, LevelManager.Tile2ID);
        if (bytes != null) {
            File.WriteAllBytes(LevelManager.GetMapPathFull(level), bytes);
        }
#if UNITY_EDITOR
        if (!Application.isPlaying) {
            AssetDatabase.Refresh();
        }
        Debug.LogFormat("SaveLevel {0} succ",level);
#endif
    }


    public static ushort Tile2ID(TileBase tile){
#if UNITY_EDITOR
        CheckLoadTileIDMap();
#endif
        if (tile == null)
            return 0;
        if (tile2ID.TryGetValue(tile, out ushort val)) {
            return val;
        }

        return 0;
    }

    public static TileBase ID2Tile(ushort tile){
#if UNITY_EDITOR
        CheckLoadTileIDMap();
#endif
        return id2Tiles[tile];
    }

    private static TileBase LoadTile(string relPath){
        var tile = Resources.Load<TileBase>(relPath);
        return tile;
    }

    public static void CheckLoadTileIDMap(){
        if (hasLoadIDMapConfig)
            return;
        hasLoadIDMapConfig = true;

        var file = Resources.Load<TextAsset>(idMapPath);
        if (file == null) {
            Debug.LogError("CheckLoadTileIDMap:LoadFileFailed " + idMapPath);
            return;
        }

        var txt = file.text;
        var allLines = txt.Replace("\r\n", "\n").Split('\n');
        var count = allLines.Length;
        tile2ID = new Dictionary<TileBase, ushort>(count);
        int i = 0;
        try {
            for (; i < count; i++) {
                var str = allLines[i];
                if (string.IsNullOrEmpty(str.Trim())) {
                    continue;
                }

                var strs = str.Split('=');
                var id = ushort.Parse(strs[0].Trim());
                var relPath = strs[1].Trim();
                var tile = LoadTile(relPath);
                id2Tiles[id] = tile;
                tile2ID.Add(tile, id);
            }
        }
        catch (Exception e) {
            Debug.LogErrorFormat("CheckLoadTileIDMap:ParseError line = {0} str = {1} path = {2} e= {3}", i + 1,
                allLines[i],
                idMapPath, e.ToString());
            return;
        }
    }
}