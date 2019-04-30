using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.Collections;
using System;
using Random = UnityEngine.Random;

[System.Serializable]
public class GameManager : BaseManager<GameManager> {
    [Header("Transforms")] [HideInInspector]
    public Transform transParentPlayer;

    [HideInInspector] public Transform transParentEnemy;
    [HideInInspector] public Transform transParentItem;
    [HideInInspector] public Transform transParentBullet;

    [Header("SpawnerInfos")] public float bornEnemyInterval = 3;
    private float bornTimer;
    public int MAX_ENEMY_COUNT = 6;
    public int initEnemyCount = 20;

    [Header("GameStatus")] [SerializeField]
    private int _RemainEnemyCount;

    public int RemainEnemyCount {
        get { return _RemainEnemyCount; }
        set {
            _RemainEnemyCount = value;
            if (OnEnmeyCountChanged != null) OnEnmeyCountChanged(_RemainEnemyCount);
        }
    }

    [SerializeField] private int _RemainPlayerLife;

    public int RemainPlayerLife {
        get { return _RemainPlayerLife; }
        set {
            _RemainPlayerLife = value;
            if (OnLifeCountChanged != null) OnLifeCountChanged(_RemainPlayerLife);
        }
    }

    public int Score = 0;
    public int CurLevel = 0;
    public bool IsGameOver = false;
    public int MAX_LEVEL_COUNT = 2;

    [Header("Prefabs")] public List<GameObject> tankPrefabs = new List<GameObject>();
    public List<GameObject> bulletPrefabs = new List<GameObject>();
    public List<GameObject> itemPrefabs = new List<GameObject>();
    public GameObject CampPrefab;
    public GameObject BornPrefab;
    public GameObject DiedPrefab;

    [Header("References")] public List<Tank> allEnmey = new List<Tank>();
    public List<Tank> allPlayer = new List<Tank>();
    public List<Bullet> allBullet = new List<Bullet>();
    public List<Item> allItem = new List<Item>();
    public Camp camp;
    public Tank myPlayer;

    [Header("MapInfos")]
    //大本营
    public BoundsInt campBound;

    public List<Vector2Int> enemyBornPoints = new List<Vector2Int>();
    public Vector2Int playerBornPoint;
    public Vector2Int player2BornPoint;
    public Vector2Int min;
    public Vector2Int max;

    [Header("Events")] public Action<int> OnLifeCountChanged;
    public Action<int> OnEnmeyCountChanged;
    public Action<int> OnLevelChanged;
    public Action<int> OnScoreChanged;
    public Action<string> OnMessage;

    //const variables
    public static Vector2 TankBornOffset = Vector2.one;
    public static float TankBornDelay = 1f;


    public override void DoAwake(){
        CurLevel = PlayerPrefs.GetInt("GameLevel", 0);
        transParentPlayer = CreateChildTrans("Players");
        transParentEnemy = CreateChildTrans("Enemies");
        transParentItem = CreateChildTrans("Items");
        transParentBullet = CreateChildTrans("Bullets");
    }

    public void CreateEnemy(Vector2Int pos, int type){
        StartCoroutine(YieldCreateEnemy(pos, type));
    }

    public void CreatePlayer(Vector2Int pos, int type){
        StartCoroutine(YiledCreatePlayer(pos, type));
    }

    public IEnumerator YieldCreateEnemy(Vector2Int pos, int type){
        ShowBornEffect(pos + TankBornOffset);
        yield return new WaitForSeconds(TankBornDelay);
        var unit = CreateUnit(pos, tankPrefabs, type, TankBornOffset, transParentEnemy, EDir.Down, allEnmey);
        unit.camp = Global.EnemyCamp;
        RemainEnemyCount--;
    }

    public IEnumerator YiledCreatePlayer(Vector2Int pos, int type){
        ShowBornEffect(pos + TankBornOffset);
        AudioManager.PlayClipBorn();
        yield return new WaitForSeconds(TankBornDelay);
        var unit = CreateUnit(pos, tankPrefabs, type, TankBornOffset, transParentPlayer, EDir.Up, allPlayer);
        unit.camp = Global.PlayerCamp;

        myPlayer = unit;
        myPlayer.name = "PlayerTank";
        RemainPlayerLife--;
    }

    public void ShowBornEffect(Vector2 pos){
        GameObject.Instantiate(BornPrefab, transform.position + new Vector3(pos.x, pos.y), Quaternion.identity);
    }

    public void ShowDiedEffect(Vector2 pos){
        GameObject.Instantiate(DiedPrefab, transform.position + new Vector3(pos.x, pos.y), Quaternion.identity);
    }


    public Bullet CreateBullet(Vector2 pos, EDir dir, Vector2 offset, int type){
        return CreateUnit(pos, bulletPrefabs, type, offset, transParentBullet, dir, allBullet);
    }

    public void CreateItem(Vector2Int pos, Vector2 offset, int type){
        CreateUnit(pos, itemPrefabs, type, offset, transParentBullet, EDir.Up, allBullet);
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
        if (unit is Tank) {
            unit.size = Vector2.one;
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
            var tank = unit as Tank;
            if (tank != null) {
                ShowDiedEffect(unit.pos);
                AudioManager.PlayClipDied();
                if (tank.camp == Global.EnemyCamp) {
                    Score += 100;
                    if (OnScoreChanged != null) {
                        OnScoreChanged(Score);
                    }
                }
            }

            unit.DoDestroy();
        }
    }


    private void ColliderDetected(){
        // update Bounding box

        HashSet<Unit> tempLst = new HashSet<Unit>();
        // bullet and tank
        foreach (var bullet in allBullet) {
            var bulletCamp = bullet.camp;
            foreach (var tank in allPlayer) {
                if (tank.camp != bulletCamp && IsCollided(bullet, tank)) {
                    tempLst.Add(bullet);
                    AudioManager.PlayClipHitTank();
                    if (tank.TakeDamage(bullet)) {
                        tempLst.Add(tank);
                    }
                }
            }

            foreach (var tank in allEnmey) {
                if (tank.camp != bulletCamp && IsCollided(bullet, tank)) {
                    tempLst.Add(bullet);
                    AudioManager.PlayClipHitTank();
                    if (tank.TakeDamage(bullet)) {
                        tempLst.Add(tank);
                    }
                }
            }
        }

        // bullet and camp
        foreach (var bullet in allBullet) {
            var bulletCamp = bullet.camp;
            if (IsCollided(bullet, camp)) {
                tempLst.Add(camp);
                break;
            }
        }

        // bullet and map
        foreach (var bullet in allBullet) {
            var pos = bullet.pos;
            Vector2 borderDir = Unit.GetBorderDir(bullet.dir);
            var borderPos1 = pos + borderDir * bullet.radius;
            var borderPos2 = pos - borderDir * bullet.radius;
            CheckBulletWithMap(pos, tempLst, bullet);
            CheckBulletWithMap(borderPos1, tempLst, bullet);
            CheckBulletWithMap(borderPos2, tempLst, bullet);
        }

        // bullet bound detected 
        foreach (var bullet in allBullet) {
            if (IsOutOfBound(bullet.pos)) {
                tempLst.Add(bullet);
            }
        }

        // tank  and item
        foreach (var unit in tempLst) {
            GameManager.Instance.DestroyUnit(unit as Bullet, GameManager.Instance.allBullet);
            GameManager.Instance.DestroyUnit(unit as Tank, GameManager.Instance.allPlayer);
            GameManager.Instance.DestroyUnit(unit as Tank, GameManager.Instance.allEnmey);
            GameManager.Instance.DestroyUnit(unit,ref camp);
        }

        if (allPlayer.Count == 0 && RemainPlayerLife <= 0) {
            GameFalied();
        }

        if (allEnmey.Count == 0 && RemainEnemyCount <= 0) {
            GameWin();
        }

        if (camp == null) {
            GameFalied();
        }
    }

    private void GameFalied(){
        IsGameOver = true;
        ShowMessage("Game Falied!!");
        Clear();
    }

    private void GameWin(){
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

    void CheckBulletWithMap(Vector2 pos, HashSet<Unit> tempLst, Unit bullet){
        var id = LevelManager.Instance.Pos2TileID(pos, true);
        if (id != 0) {
            //collide bullet with world
            if (id == Global.TileID_Brick) {
                if (bullet.camp == Global.PlayerCamp) {
                    AudioManager.PlayClipHitBrick();
                }

                LevelManager.Instance.ReplaceTile(pos, id, 0);
                tempLst.Add(bullet);
            }

            if (id == Global.TileID_Iron || id == Global.TileID_Wall) {
                if (id == Global.TileID_Iron && bullet.camp == Global.PlayerCamp) {
                    AudioManager.PlayClipHitIron();
                }

                tempLst.Add(bullet);
            }
        }
    }


    public bool IsOutOfBound(Vector2 fpos){
        var pos = LevelManager.Pos2TilePos(fpos);
        if (pos.x < min.x || pos.x > max.x
                          || pos.y < min.y || pos.y > max.y
        ) {
            return true;
        }

        return false;
    }

    public bool IsCollided(Vector2 posA, float rA, Vector2 sizeA, Vector2 posB, float rB, Vector2 sizeB){
        var diff = posA - posB;
        var allRadius = rA + rB;
        //circle 判定
        if (diff.sqrMagnitude > allRadius * allRadius) {
            return false;
        }

        var isBoxA = sizeA != Vector2.zero;
        var isBoxB = sizeB != Vector2.zero;
        if (!isBoxA && !isBoxB)
            return true;
        var absX = Mathf.Abs(diff.x);
        var absY = Mathf.Abs(diff.y);
        if (isBoxA && isBoxB) {
            //AABB and AABB
            var allSize = sizeA + sizeB;
            if (allSize.x > absX) return false;
            if (allSize.y > absY) return false;
            return true;
        }
        else {
            //AABB & circle
            var size = sizeB;
            var radius = rA;
            if (isBoxA) {
                size = sizeA;
                radius = rB;
            }

            var x = Mathf.Max(absX - size.x, 0);
            var y = Mathf.Max(absY - size.y, 0);
            return x * x + y * y < radius * radius;
        }
    }

    public bool IsCollided(Unit a, Unit b){
        return IsCollided(a.pos, a.radius, a.size, b.pos, b.radius, b.size);
    }

    public bool IsCollider(Unit a, Vector2Int tilePos){
        return IsCollided(a.pos, a.radius, a.size,
            tilePos + Vector2.one * 0.5f,
            0.7072f, Vector2.one * 0.5f);
    }

    private Transform CreateChildTrans(string name){
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        return go.transform;
    }

    public void Clear(){
        Clear(allEnmey);
        Clear(allPlayer);
        Clear(allBullet);
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

    private void DestoryUnit<T>(ref T unit) where T : Unit{
        if (unit != null) {
            GameObject.Destroy(unit.gameObject);
            unit = null;
        }
    }

    /// <summary>
    /// 正式开始游戏
    /// </summary>
    public void StartGame(int level){
        //reset variables
        CurLevel = level;
        IsGameOver = false;
        bornTimer = 0;
        RemainEnemyCount = initEnemyCount;
        camp = null;
        myPlayer = null;

        var tileInfo = main.levelMgr.GetMapInfo(Global.TileMapName_BornPos);
        var campPoss = tileInfo.GetAllTiles(LevelManager.ID2Tile(Global.TileID_Camp));
        Debug.Assert(campPoss != null && campPoss.Count == 1, "campPoss!= null&& campPoss.Count == 1");
        enemyBornPoints = tileInfo.GetAllTiles(LevelManager.ID2Tile(Global.TileID_BornPosEnemy));
        var heroBornPoss = tileInfo.GetAllTiles(LevelManager.ID2Tile(Global.TileID_BornPosHero));
        playerBornPoint = heroBornPoss[0];
        if (heroBornPoss.Count > 1) {
            player2BornPoint = heroBornPoss[1];
        }

        AudioManager.PlayMusicStart();
        //create players
        CreatePlayer(playerBornPoint, 0);
        //create camps
        var pos = (campPoss[0] + Vector2.one);
        camp = GameObject.Instantiate(CampPrefab,transform.position+ (Vector3)pos, Quaternion.identity, transParentItem.parent)
            .GetComponent<Camp>();
        camp.pos = pos;
        camp.size = Vector2.one;
        camp.radius = camp.size.magnitude;
    }


    public override void DoUpdate(float deltaTime){
        if (IsGameOver) return;
        //update player dir
        if (myPlayer != null) {
            var input = main.inputMgr;
            var v = input.vertical;
            var h = input.horizontal;
            var absh = Mathf.Abs(h);
            var absv = Mathf.Abs(v);
            if (absh < 0.01f && absv < 0.01f) {
                myPlayer.moveSpd = 0;
            }
            else {
                myPlayer.dir = absh > absv
                    ? (h < 0 ? EDir.Left : EDir.Right)
                    : (v < 0 ? EDir.Down : EDir.Up);
                myPlayer.moveSpd = myPlayer.maxMoveSpd;
            }

            var isFire = input.firePressed || input.fireHeld;
            if (isFire) {
                myPlayer.Fire();
            }
        }

        //born enemy
        if (allEnmey.Count < MAX_ENEMY_COUNT && RemainEnemyCount > 0) {
            bornTimer += deltaTime;
            if (bornTimer > bornEnemyInterval && enemyBornPoints.Count > 0) {
                bornTimer = 0;
                //born enemy
                var idx = Random.Range(0, enemyBornPoints.Count);
                var bornPoint = enemyBornPoints[idx];
                CreateEnemy(bornPoint, 0);
            }
        }

        //update pos
        foreach (var target in allEnmey) {
            target.DoUpdate(deltaTime);
        }

        foreach (var target in allPlayer) {
            target.DoUpdate(deltaTime);
        }

        foreach (var target in allBullet) {
            target.DoUpdate(deltaTime);
        }

        //collision detection
        ColliderDetected();
        //
    }
}