using UnityEngine;
using System.Collections.Generic;
[System.Serializable]
public class GameManager : BaseManager {
    public int CurLevel = 0;

    public override void DoAwake(){
        //TODO load from config
        CurLevel = PlayerPrefs.GetInt("GameLevel", 0);
    }

    /// <summary>
    /// 正式开始游戏
    /// </summary>
    public void StartGame(){
        // 
        var go = GameObject.Find("Grid");
        var grid = go.GetComponent<Grid>();
    }
    
    
    public override void DoUpdate(float deltaTime){
        
    }
    
    public List<Vector2Int> enemyBornPoints = new List<Vector2Int>();
    public Vector2Int playerBornPoint;
    public Vector2Int player2BornPoint;
    //大本营
    public BoundsInt campBound;
    
    
}