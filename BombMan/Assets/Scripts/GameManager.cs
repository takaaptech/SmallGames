using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.Collections;
using System;
using Random = UnityEngine.Random;

[System.Serializable]
public class PlayerInfo {
    public bool isMainPlayer = false;
    public Vector2Int bornPos;
    public Walker walker;
    public bool isLiveInLastLevel = true;
    public int lastLevelTankType;
    public int score;
    public int remainPlayerLife;
}

[System.Serializable]
public partial class GameManager : BaseManager<GameManager> {
    [Header("Transforms")] [HideInInspector]
    public Transform transParentPlayer;

    [HideInInspector] public Transform transParentEnemy;
    [HideInInspector] public Transform transParentItem;
    [HideInInspector] public Transform transParentBullet;

    [Header("SpawnerInfos")] public float bornEnemyInterval = 3;
    private float bornTimer;
    public int MAX_ENEMY_COUNT = 6;
    public int initEnemyCount = 20;
    public LevelManager levelMgr;

    [Header("GameStatus")] [SerializeField]
    private int _RemainEnemyCount;

    public int RemainEnemyCount {
        get { return _RemainEnemyCount; }
        set {
            _RemainEnemyCount = value;
            if (OnEnmeyCountChanged != null) OnEnmeyCountChanged(_RemainEnemyCount);
        }
    }

    public int CurLevel = 0;
    public bool IsGameOver = false;
    public int MAX_LEVEL_COUNT = 2;

    [Header("Prefabs")] public List<GameObject> tankPrefabs = new List<GameObject>();
    public List<GameObject> playerPrefabs = new List<GameObject>();
    public List<GameObject> bulletPrefabs = new List<GameObject>();
    public List<GameObject> itemPrefabs = new List<GameObject>();
    public GameObject CampPrefab;
    public GameObject BornPrefab;
    public GameObject DiedPrefab;

    [Header("References")] public List<Walker> allEnmey = new List<Walker>();
    public List<Walker> allPlayer = new List<Walker>();
    public List<Bomb> allBomb = new List<Bomb>();
    public List<Item> allItem = new List<Item>();
    public Camp camp;
    public Walker myPlayer;
    public Walker player2;

    [Header("MapInfos")]
    //大本营
    public BoundsInt campBound;

    public List<Vector2Int> enemyBornPoints = new List<Vector2Int>();
    public Vector2Int min;
    public Vector2Int max;

    [Header("Events")] public Action<PlayerInfo> OnLifeCountChanged;
    public Action<int> OnEnmeyCountChanged;
    public Action<int> OnLevelChanged;
    public Action<PlayerInfo> OnScoreChanged;
    public Action<string> OnMessage;

    //const variables
    public static float TankBornDelay = 1f;


    public const int MAX_PLAYER_COUNT = 2;
    public PlayerInfo[] allPlayerInfos = new PlayerInfo[MAX_PLAYER_COUNT];

    HashSet<Vector2Int> tempPoss = new HashSet<Vector2Int>();
    HashSet<Unit> tempLst = new HashSet<Unit>();
    public Vector2Int campPos;
    public bool HasCreatedCamp = false;


    #region LifeCycle

    public override void DoAwake(){
        CurLevel = PlayerPrefs.GetInt("GameLevel", 0);
        Func<string, Transform> FuncCreateTrans = (name) => {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go.transform;
        };
        transParentPlayer = FuncCreateTrans("Players");
        transParentEnemy = FuncCreateTrans("Enemies");
        transParentItem = FuncCreateTrans("Items");
        transParentBullet = FuncCreateTrans("Bomb");
    }

    /// <summary>
    /// 正式开始游戏
    /// </summary>
    public void StartGame(int level){
        levelMgr = LevelManager.Instance;
        //reset variables
        CurLevel = level;
        IsGameOver = false;
        bornTimer = 0;
        RemainEnemyCount = initEnemyCount;
        camp = null;
        HasCreatedCamp = false;
        //read map info
        var tileInfo = main.levelMgr.GetMapInfo(Global.TileMapName_BornPos);
        var campPoss = tileInfo.GetAllTiles(LevelManager.ID2Tile(Global.TileID_Camp));
        Debug.Assert(campPoss != null && campPoss.Count == 1, "campPoss!= null&& campPoss.Count == 1");
        enemyBornPoints = tileInfo.GetAllTiles(LevelManager.ID2Tile(Global.TileID_BornPosEnemy));
        var bornPoss = tileInfo.GetAllTiles(LevelManager.ID2Tile(Global.TileID_BornPosHero));
        Debug.Assert(bornPoss.Count == MAX_PLAYER_COUNT, "Map should has 2 player born pos");
        // init player pos info
        foreach (var info in allPlayerInfos) {
            if (info != null) {
                info.walker = null;
            }
        }

        allPlayerInfos[0].isMainPlayer = true;
        for (int i = 0; i < MAX_PLAYER_COUNT; i++) {
            var playerInfo = allPlayerInfos[i];
            if (playerInfo != null) {
                playerInfo.bornPos = bornPoss[i];
            }
        }

        //create players
        for (int i = 0; i < MAX_PLAYER_COUNT; i++) {
            var playerInfo = allPlayerInfos[i];
            if (playerInfo != null && !(playerInfo.remainPlayerLife == 0 && !playerInfo.isLiveInLastLevel)) {
                CreatePlayer(playerInfo, playerInfo.lastLevelTankType, false);
            }
        }

        //create enemy 
        initEnemyCount = enemyBornPoints.Count;
        for (int i = 0; i < initEnemyCount; i++) {
            var idx = Random.Range(0, enemyBornPoints.Count);
            var bornPoint = enemyBornPoints[idx];
            CreateEnemy(bornPoint, 0);
        }

        AudioManager.PlayMusicStart();
        campPos = campPoss[0];
    }


    public PlayerInfo GetPlayerFormTank(Walker walker){
        if (walker == null) return null;
        foreach (var info in allPlayerInfos) {
            if (info != null && info.walker == walker) {
                return info;
            }
        }

        return null;
    }

    void UpdatePlayer(PlayerInfo playerInfo, InputInfo inputInfo){
        float inputV = inputInfo.vertical;
        float intputH = inputInfo.horizontal;
        bool isFirePressed = inputInfo.firePressed;
        bool isFireHeld = inputInfo.fireHeld;

        var tank = playerInfo.walker;
        if (tank != null) {
            var input = main.inputMgr;
            var v = inputV;
            var h = intputH;
            var absh = Mathf.Abs(h);
            var absv = Mathf.Abs(v);
            if (absh < 0.01f && absv < 0.01f) {
                tank.moveSpd = 0;
            }
            else {
                tank.dir = absh > absv
                    ? (h < 0 ? EDir.Left : EDir.Right)
                    : (v < 0 ? EDir.Down : EDir.Up);
                tank.moveSpd = tank.MaxMoveSpd;
            }

            var isFire = isFirePressed || isFireHeld;
            if (isFire) {
                tank.Fire();
            }
        }
    }

    public override void DoUpdate(float deltaTime){
        if (IsGameOver) return;
        //update player dir from input command
        var input = main.inputMgr;
        UpdatePlayer(allPlayerInfos[0], input.inputs[0]);
        UpdatePlayer(allPlayerInfos[1], input.inputs[1]);
        //update all units
        foreach (var target in allEnmey) {
            target.DoUpdate(deltaTime);
        }

        foreach (var target in allPlayer) {
            target.DoUpdate(deltaTime);
        }

        foreach (var target in allBomb) {
            target.DoUpdate(deltaTime);
        }

        //collision detection
        ColliderDetected();
    }

    private void ColliderDetected(){
        // update Bounding box

        // bullet and tank
        foreach (var enemy in allEnmey) {
            foreach (var player in allPlayer) {
                if (CollisionHelper.CheckCollision(enemy, player)) {
                    tempLst.Add(player);
                    AudioManager.PlayClipHitTank();
                }
            }
        }

        // tank  and item
        var players = allPlayer.ToArray(); //item may modified the allPlayer list so copy it
        foreach (var player in players) {
            foreach (var item in allItem) {
                if (CollisionHelper.CheckCollision(player, item)) {
                    item.TriggelEffect(player);
                    tempLst.Add(item);
                }
            }
        }

        //Apply Bomb Effect
        foreach (var bomb in allBomb) {
            if (bomb.isExploded) {
                ApplyExplodeCross(bomb);
            }
        }
        //连炸效果
        while (pendingExplodeBombs.Count > 0) {
            var bomb = pendingExplodeBombs.Dequeue();
            ApplyExplodeCross(bomb);
        }
        explodedPoss.Clear();
        explodeEffectPos.Clear();

        
        foreach (var unit in allEnmey) {
            if (unit.health <= 0) {
                tempLst.Add(unit);
            }
        }

        foreach (var unit in allPlayer) {
            if (unit.health <= 0) {
                tempLst.Add(unit);
            }
        }

        // destroy unit
        foreach (var unit in tempLst) {
            GameManager.Instance.DestroyUnit(unit as Bomb, GameManager.Instance.allBomb);
            GameManager.Instance.DestroyUnit(unit as Walker, GameManager.Instance.allPlayer);
            GameManager.Instance.DestroyUnit(unit as Walker, GameManager.Instance.allEnmey);
            GameManager.Instance.DestroyUnit(unit as Item, GameManager.Instance.allItem);
            GameManager.Instance.DestroyUnit(unit, ref camp);
        }

        if (allPlayer.Count == 0) {
            bool hasNoLife = true;
            foreach (var info in allPlayerInfos) {
                if (info != null && info.remainPlayerLife > 0) {
                    hasNoLife = false;
                    break;
                }
            }

            if (hasNoLife) {
                GameFalied();
            }
        }

        if (allEnmey.Count == 0 && RemainEnemyCount <= 0) {
            if (!HasCreatedCamp) {
                CreateCamp();
            }
        }

        if (camp != null) {
            foreach (var player in allPlayer) {
                if (CollisionHelper.CheckCollision(camp, player)) {
                    GameWin();
                    break;
                }
            }
        }

        tempLst.Clear();
    }

    //dangqianz
    private HashSet<Vector2Int> explodedPoss = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> explodeEffectPos = new HashSet<Vector2Int>();
    private Queue<Bomb> pendingExplodeBombs = new Queue<Bomb>();

    public void ApplyExplodeCross(Bomb bomb){
        AudioManager.PlayClipExploded();
        var dist = bomb.damageRange;
        var rawPos = bomb.pos.Floor();
        tempLst.Add(bomb);
        bomb.isExploded = true;
        explodedPoss.Add(rawPos);
        //Check 4 dir
        for (int i = 0; i < (int) EDir.EnumCount; i++) {
            var dirVec = CollisionHelper.GetDirVec((EDir) i);
            ApplyExplodeLine(bomb, rawPos, dirVec);
        }

        //check the centerPos
        ApplyExplodePoint(bomb, rawPos);
    }

    void ApplyExplodeLine(Bomb bomb, Vector2Int rawPos, Vector2Int dirVec){
        var dist = bomb.damageRange;
        for (int i = 1; i <= dist; i++) {
            var iPos = rawPos + dirVec * i;
            if (ApplyExplodePoint(bomb, iPos)) {
                break;
            }
        }
    }

    private bool ApplyExplodePoint(Bomb bomb, Vector2Int iPos){
        var id = levelMgr.Pos2TileID(iPos);
        if (id == Global.TileID_Iron) {
            //停下来
            return true;
        }

        else if (id == Global.TileID_Brick) {
            LevelManager.Instance.ReplaceTile(iPos, id, 0);
        }
        else if (id == 0) {
            var allWalkers = GetWalkerFormPos(iPos);
            foreach (var walker in allWalkers) {
                if (walker.health > 0) {
                    walker.health = 0;
                    walker.killer = bomb.owner;
                }
            }

            //连炸
            foreach (var oBomb in allBomb) {
                if (oBomb.pos.Floor() == iPos) {
                    if (explodedPoss.Add(iPos)) {
                        pendingExplodeBombs.Enqueue(oBomb);
                    }
                }
            }
        }
        if (explodeEffectPos.Add(iPos)) {
            ShowDiedEffect(iPos + Global.UnitSizeVec);
        }
        return false;
    }

    private List<Walker> tempList = new List<Walker>();

    public List<Walker> GetWalkerFormPos(Vector2Int iPos){
        tempList.Clear();
        foreach (var walker in allEnmey) {
            if (CollisionHelper.CheckCollision(walker, iPos)) {
                tempList.Add(walker);
            }
        }

        foreach (var walker in allPlayer) {
            if (CollisionHelper.CheckCollision(walker, iPos)) {
                tempList.Add(walker);
            }
        }

        return tempList;
    }

    #endregion

    #region GameStatus

    private void GameFalied(){
        IsGameOver = true;
        ShowMessage("Game Falied!!");
        Clear();
    }

    private void GameWin(){
        foreach (var playerInfo in allPlayerInfos) {
            if (playerInfo != null) {
                playerInfo.lastLevelTankType = playerInfo.walker == null ? 0 : playerInfo.walker.detailType;
                playerInfo.isLiveInLastLevel = playerInfo.walker != null;
            }
        }

        IsGameOver = true;
        if (CurLevel >= MAX_LEVEL_COUNT) {
            ShowMessage("You Win!!");
        }
        else {
            Clear();
            LevelManager.Instance.LoadGame(CurLevel + 1);
        }
    }


    private void ShowMessage(string str){
        if (OnMessage != null) {
            OnMessage(str);
        }
    }

    private void Clear(){
        Clear(allEnmey);
        Clear(allPlayer);
        Clear(allBomb);
        Clear(allItem);
        DestoryUnit(ref myPlayer);
        DestoryUnit(ref camp);
    }

    private void Clear<T>(List<T> lst) where T : Unit{
        foreach (var unit in lst) {
            if (unit != null) {
                GameObject.Destroy(unit.gameObject);
            }
        }

        lst.Clear();
    }

    #endregion

    #region Create& Destroy

    public bool Upgrade(Walker playerWalker, int upLevel = 1){
        var level = playerWalker.detailType + 1;
        if (level >= playerPrefabs.Count) {
            return false;
        }

        var playerInfo = GetPlayerFormTank(playerWalker);
        // TODO 最好不要这样做 而是以直接修改属性的方式 
        var tank = DirectCreatePlayer(playerWalker.pos - Global.UnitSizeVec, level, playerInfo, false);
        allPlayer.Remove(playerWalker);
        GameObject.Destroy(playerWalker.gameObject);
        return true;
    }


    public Camp CreateCamp(){
        var pos = (campPos + Global.UnitSizeVec);
        camp = GameObject.Instantiate(CampPrefab, transform.position + (Vector3) pos, Quaternion.identity,
                transParentItem.parent)
            .GetComponent<Camp>();
        camp.pos = pos;
        camp.size = Global.UnitSizeVec;
        camp.radius = camp.size.magnitude;
        return camp;
    }

    public void CreateEnemy(Vector2Int pos, int type){
        StartCoroutine(YieldCreateEnemy(pos, type));
    }

    public void CreatePlayer(PlayerInfo playerInfo, int type = 0, bool isConsumeLife = true){
        StartCoroutine(YiledCreatePlayer(playerInfo.bornPos, type, playerInfo, isConsumeLife));
    }

    public IEnumerator YieldCreateEnemy(Vector2Int pos, int type){
        ShowBornEffect(pos + Global.UnitSizeVec);
        yield return new WaitForSeconds(TankBornDelay);
        var unit = CreateUnit(pos, tankPrefabs, type, Global.UnitSizeVec, transParentEnemy, EDir.Down, allEnmey);
        unit.camp = Global.EnemyCamp;
        RemainEnemyCount--;
    }

    public IEnumerator YiledCreatePlayer(Vector2 pos, int type, PlayerInfo playerInfo, bool isConsumeLife){
        ShowBornEffect(pos + Global.UnitSizeVec);
        AudioManager.PlayClipBorn();
        yield return new WaitForSeconds(TankBornDelay);
        DirectCreatePlayer(pos, type, playerInfo, isConsumeLife);
    }

    private Walker DirectCreatePlayer(Vector2 pos, int type, PlayerInfo playerInfo, bool isConsumeLife){
        var unit = CreateUnit(pos, playerPrefabs, type, Global.UnitSizeVec, transParentPlayer, EDir.Up, allPlayer);
        unit.camp = Global.PlayerCamp;
        unit.brain.enabled = false;
        unit.name = "PlayerTank";
        playerInfo.walker = unit;
        if (isConsumeLife) {
            playerInfo.remainPlayerLife--;
        }

        if (OnLifeCountChanged != null) {
            OnLifeCountChanged(playerInfo);
        }

        return unit;
    }

    public void ShowBornEffect(Vector2 pos){
        GameObject.Instantiate(BornPrefab, transform.position + new Vector3(pos.x, pos.y), Quaternion.identity);
    }

    public void ShowDiedEffect(Vector2 pos){
        GameObject.Instantiate(DiedPrefab, transform.position + new Vector3(pos.x, pos.y), Quaternion.identity);
    }


    public Bomb CreateBomb(Vector2 pos, EDir dir, Vector2 offset, int type){
        return CreateUnit(pos, bulletPrefabs, type, offset, transParentBullet, dir, allBomb);
    }

    public void CreateItem(Vector2 pos, int type){
        CreateUnit(pos, itemPrefabs, type, Vector2.one, transParentItem, EDir.Up, allItem);
    }


    private T CreateUnit<T>(Vector2 pos, List<GameObject> lst, int type,
        Vector2 offset, Transform parent, EDir dir,
        List<T> set) where T : Unit{
        Debug.Assert(type <= lst.Count, "type >= lst.Count");
        var prefab = lst[type];
        Debug.Assert(prefab != null, "prefab == null");
        Vector2 createPos = pos + offset;

        var deg = ((int) (dir)) * 90;
        var rotation = Quaternion.Euler(0, 0, deg);

        var go = GameObject.Instantiate(prefab, parent.position + (Vector3) createPos, rotation, parent);
        var unit = go.GetComponent<T>();
        unit.pos = createPos;
        unit.dir = dir;
        unit.detailType = type;
        if (unit is Walker) {
            unit.size = Global.UnitSizeVec;
            unit.radius = unit.size.magnitude;
        }

        if (unit is Item) {
            unit.size = Global.UnitSizeVec;
            unit.radius = unit.size.magnitude;
        }

        unit.DoStart();
        set.Add(unit);
        return unit;
    }

    public void DestroyUnit<T>(Unit unit, ref T rUnit) where T : Unit{
        if (unit is T) {
            GameObject.Destroy(unit.gameObject);
            ShowDiedEffect(unit.pos);
            AudioManager.PlayClipDied();
            unit.DoDestroy();
            rUnit = null;
        }
    }

    public void DestroyUnit<T>(T unit, List<T> lst) where T : Unit{
        if (lst.Remove(unit)) {
            var tank = unit as Walker;
            if (tank != null) {
                ShowDiedEffect(unit.pos);
                AudioManager.PlayClipDied();
                if (tank.camp == Global.EnemyCamp) {
                    var info = GetPlayerFormTank(tank.killer);
                    info.score += (tank.detailType + 1) * 100;

                    if (OnScoreChanged != null) {
                        OnScoreChanged(info);
                    }

                    if ( //tank.detailType >= Global.ItemTankType &&
                        itemPrefabs.Count > 0) {
                        var x = Random.Range(min.x + 1.0f, max.x - 3.0f);
                        var y = Random.Range(min.y + 1.0f, max.y - 3.0f);
                        CreateItem(new Vector2(x, y), Random.Range(0, itemPrefabs.Count));
                    }
                }

                if (tank.camp == Global.PlayerCamp) {
                    var info = GetPlayerFormTank(tank);
                    Debug.Assert(info != null, " player's tank have no owner");
                    if (info.remainPlayerLife > 0) {
                        CreatePlayer(info, 0);
                    }
                }
            }


            unit.DoDestroy();
        }
    }

    private void DestoryUnit<T>(ref T unit) where T : Unit{
        if (unit != null) {
            GameObject.Destroy(unit.gameObject);
            unit = null;
        }
    }

    #endregion
}