using UnityEngine;

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
}