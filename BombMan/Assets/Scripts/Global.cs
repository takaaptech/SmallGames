using UnityEngine;
public static class Global {
    public const string TileMapName_BornPos = "BornPos";
    public const int TileID_Brick = 1;
    public const int TileID_Iron = 2;
    public const int TileID_Camp = 3;
    public const int TileID_BornPosEnemy = 4;
    public const int TileID_BornPosHero = 5;
    public static int CurGameLevel;
    
    
    public const int ItemTankType = 4;
    public const int EnemyCamp = 1;
    public const int PlayerCamp = 2;
    
    
    public const float UnitSize = 0.5f;
    public static Vector2 UnitSizeVec = new Vector2(UnitSize,UnitSize);
}