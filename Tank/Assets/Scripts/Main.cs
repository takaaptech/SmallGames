using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 1.游戏生命期管理
/// 2.关卡加载
/// while(true)
/// {
/// 3.处理输入
/// 4.游戏逻辑
///    4.1纯逻辑
///       攻击
///       被攻击
///       分数累计
///       胜负判定
///    4.2视觉展示
///       动画 特效  UI
///    4.3听觉输出
///       音效 背景音乐之类的
/// }
/// 5.结果展示
/// if(继续)
///     goto 2
/// else
///     exit game
/// </summary>
public class Main : UnityEngine.MonoBehaviour {
    public static AudioManager audioMgr { get; private set; }
    public static InputManager inputMgr { get; private set; }
    public static LevelManager levelMgr { get; private set; }
    public static GameManager gameMgr { get; private set; }
    public static Main Instance { get; private set; }
    private List<BaseManager> allMgrs = new List<BaseManager>();
    private string curLevelName = "";

    public static bool IsGameOver(){
        return false;
    }

    private void Awake(){
        if (Instance != null) {
            Debug.LogError("Error: has 2 main scripts!!");
            GameObject.Destroy(this.gameObject);
        }

        Instance = this;
        audioMgr = new AudioManager();
        inputMgr = new InputManager();
        levelMgr = new LevelManager();
        gameMgr = new GameManager();
        //register mgrs 
        allMgrs.Add(audioMgr);
        allMgrs.Add(inputMgr);
        allMgrs.Add(levelMgr);
        allMgrs.Add(gameMgr);
        SceneManager.sceneLoaded += OnSceneLoaded;

        foreach (var mgr in allMgrs) {
            mgr.DoAwake();
        }
    }

    private void Start(){
        foreach (var mgr in allMgrs) {
            mgr.DoStart();
        }
    }

    public void Update(){
        var deltaTime = Time.deltaTime;
        foreach (var mgr in allMgrs) {
            mgr.DoUpdate(deltaTime);
        }
    }

    private void OnDestroy(){
        foreach (var mgr in allMgrs) {
            mgr.DoDestroy();
        }
    }

    public void LoadScene(string name){
        if (string.IsNullOrEmpty(curLevelName)) {
            SceneManager.UnloadSceneAsync(name);
        }

        SceneManager.LoadScene(name, LoadSceneMode.Additive);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode){
        if (name.StartsWith("Level")) {
            curLevelName = scene.name;
        }

        levelMgr.OnSceneLoaded();
    }
}

public class BaseManager {
    public virtual void DoAwake(){ }
    public virtual void DoStart(){ }
    public virtual void DoUpdate(float deltaTime){ }

    public virtual void DoFixedUpdate(){}
    public virtual void DoDestroy(){ }
}