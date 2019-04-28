using System.Collections.Generic;
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
public class Main : MonoBehaviour {
    public AudioManager audioMgr;
    public InputManager inputMgr;
    public LevelManager levelMgr;
    public GameManager gameMgr;
    public static Main Instance { get; private set; }
    private List<BaseMgr> allMgrs = new List<BaseMgr>();
    private string curLevelName = "";


    public static bool IsGameOver(){
        return false;
    }

    private void Start(){
        if (Instance != null) {
            Debug.LogError("Error: has 2 main scripts!!");
            GameObject.Destroy(this.gameObject);
        }

        Instance = this;
        audioMgr = AudioManager.Instance;
        inputMgr = InputManager.Instance;
        levelMgr = LevelManager.Instance;
        gameMgr = GameManager.Instance;
        //register mgrs 
        allMgrs.Add(audioMgr);
        allMgrs.Add(inputMgr);
        allMgrs.Add(levelMgr);
        allMgrs.Add(gameMgr);
        //SceneManager.sceneLoaded += OnSceneLoaded;
        foreach (var mgr in allMgrs) {
            mgr.Init(this);
        }

        foreach (var mgr in allMgrs) {
            mgr.DoAwake();
        }

        foreach (var mgr in allMgrs) {
            mgr.DoStart();
        }
        levelMgr.OnSceneLoaded();
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
}

public class BaseMgr : MonoBehaviour {
    public void Init(Main main){
        this.main = main;
    }

    public Main main { get; private set; }
    public virtual void DoAwake(){ }
    public virtual void DoStart(){ }
    public virtual void DoUpdate(float deltaTime){ }

    public virtual void DoFixedUpdate(){ }
    public virtual void DoDestroy(){ }
}

public class BaseManager<T> : BaseMgr where T : BaseManager<T> {
    private static T _instance;

    public static T Instance {
        get {
            if (_instance == null) {
                _instance = new GameObject(typeof(T).ToString()).AddComponent<T>();
            }

            return _instance;
        }
    }

    protected void Awake(){
        if (_instance != null) {
            GameObject.Destroy(this);
            return;
        }

        _instance = (T) this;
    }
}