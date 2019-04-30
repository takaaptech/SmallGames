using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.Collections;
using System;
using Random = UnityEngine.Random;

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
    public List<GameObject> playerPrefabs = new List<GameObject>();
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
        transParentBullet = FuncCreateTrans("Bullets");
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
        CreatePlayer(playerBornPoint, lastLevelPlayerTankType);
        //create camps
        var pos = (campPoss[0] + Vector2.one);
        camp = GameObject.Instantiate(CampPrefab, transform.position + (Vector3) pos, Quaternion.identity,
                transParentItem.parent)
            .GetComponent<Camp>();
        camp.pos = pos;
        camp.size = Vector2.one;
        camp.radius = camp.size.magnitude;
    }

    public int lastLevelPlayerTankType = 0;

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
                myPlayer.moveSpd = myPlayer.MaxMoveSpd;
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
                CreateEnemy(bornPoint, Random.Range(0, tankPrefabs.Count));
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

    HashSet<Vector2Int> tempPoss = new HashSet<Vector2Int>();
    HashSet<Unit> tempLst = new HashSet<Unit>();

    private void ColliderDetected(){
        // update Bounding box

        // bullet and tank
        foreach (var bullet in allBullet) {
            var bulletCamp = bullet.camp;
            foreach (var tank in allPlayer) {
                if (tank.camp != bulletCamp && CollisionHelper.CheckCollision(bullet, tank)) {
                    tempLst.Add(bullet);
                    AudioManager.PlayClipHitTank();
                    tank.TakeDamage(bullet);
                }
            }

            foreach (var tank in allEnmey) {
                if (tank.camp != bulletCamp && CollisionHelper.CheckCollision(bullet, tank)) {
                    tempLst.Add(bullet);
                    AudioManager.PlayClipHitTank();
                    tank.TakeDamage(bullet);
                }
            }
        }

        // bullet and camp
        foreach (var bullet in allBullet) {
            var bulletCamp = bullet.camp;
            if (CollisionHelper.CheckCollision(bullet, camp)) {
                tempLst.Add(camp);
                break;
            }
        }

        // bullet and map
        foreach (var bullet in allBullet) {
            var pos = bullet.pos;
            Vector2 borderDir = CollisionHelper.GetBorderDir(bullet.dir);
            var borderPos1 = pos + borderDir * bullet.radius;
            var borderPos2 = pos - borderDir * bullet.radius;
            tempPoss.Add(pos.Floor());
            tempPoss.Add(borderPos1.Floor());
            tempPoss.Add(borderPos2.Floor());
            foreach (var iPos in tempPoss) {
                CheckBulletWithMap(iPos, tempLst, bullet);
            }

            tempPoss.Clear();
        }

        // bullet bound detected 
        foreach (var bullet in allBullet) {
            if (CollisionHelper.IsOutOfBound(bullet.pos, min, max)) {
                bullet.health = 0;
            }
        }

        // tank  and item
        var players = allPlayer.ToArray(); //item may modified the allPlayer list so copy it
        foreach (var tank in players) {
            foreach (var item in allItem) {
                if (CollisionHelper.CheckCollision(tank, item)) {
                    item.TriggelEffect(tank);
                    tempLst.Add(item);
                }
            }
        }

        foreach (var bullet in allBullet) {
            if (bullet.health <= 0) {
                tempLst.Add(bullet);
            }
        }

        foreach (var bullet in allEnmey) {
            if (bullet.health <= 0) {
                tempLst.Add(bullet);
            }
        }

        foreach (var bullet in allPlayer) {
            if (bullet.health <= 0) {
                tempLst.Add(bullet);
            }
        }

        // destroy unit
        foreach (var unit in tempLst) {
            GameManager.Instance.DestroyUnit(unit as Bullet, GameManager.Instance.allBullet);
            GameManager.Instance.DestroyUnit(unit as Tank, GameManager.Instance.allPlayer);
            GameManager.Instance.DestroyUnit(unit as Tank, GameManager.Instance.allEnmey);
            GameManager.Instance.DestroyUnit(unit as Item, GameManager.Instance.allItem);
            GameManager.Instance.DestroyUnit(unit, ref camp);
        }

        if (allPlayer.Count == 0 && RemainPlayerLife <= 0) {
            GameFalied();
        }

        if (allEnmey.Count == 0 && RemainEnemyCount <= 0) {
            lastLevelPlayerTankType = myPlayer.detailType;
            GameWin();
        }

        if (camp == null) {
            GameFalied();
        }

        tempLst.Clear();
    }


    private void CheckBulletWithMap(Vector2Int iPos, HashSet<Unit> tempLst, Bullet bullet){
        var id = LevelManager.Instance.Pos2TileID(iPos,false);
        if (id != 0 && bullet.health > 0) {
            //collide bullet with world
            if (id == Global.TileID_Brick) {
                if (bullet.camp == Global.PlayerCamp) {AudioManager.PlayClipHitBrick();}
                LevelManager.Instance.ReplaceTile(iPos, id, 0);
                bullet.health--;
            }
            else if (id == Global.TileID_Iron) {
                if (!bullet.canDestoryIron) {
                    if (bullet.camp == Global.PlayerCamp) {AudioManager.PlayClipHitIron();}
                    bullet.health = 0;
                }
                else {
                    if (bullet.camp == Global.PlayerCamp) {AudioManager.PlayClipDestroyIron();}
                    bullet.health = Mathf.Max(bullet.health - 2, 0);
                    LevelManager.Instance.ReplaceTile(iPos, id, 0);
                }
            }
            else if (id == Global.TileID_Grass) {
                if (bullet.canDestoryGrass) {
                    if (bullet.camp == Global.PlayerCamp) {AudioManager.PlayClipDestroyGrass();}
                    bullet.health -= 0;
                    LevelManager.Instance.ReplaceTile(iPos, id, 0);
                }
            }
            else if (id == Global.TileID_Wall) {
                bullet.health = 0;
            }
        }
    }

    #endregion

    #region GameStatus

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

    private void Clear(){
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

    #endregion

    #region Create& Destroy

    public bool Upgrade(Tank player, int upLevel = 1){
        var level = player.detailType + 1;
        if (level >= playerPrefabs.Count) {
            return false;
        }

        // TODO 最好不要这样做 而是以直接修改属性的方式 
        var tank = DirectCreatePlayer(player.pos - TankBornOffset, level);
        allPlayer.Remove(player);
        GameObject.Destroy(player.gameObject);
        return true;
    }

    public void CreateEnemy(Vector2Int pos, int type){
        StartCoroutine(YieldCreateEnemy(pos, type));
    }

    public void CreatePlayer(Vector2 pos, int type){
        StartCoroutine(YiledCreatePlayer(pos, type));
    }

    public IEnumerator YieldCreateEnemy(Vector2Int pos, int type){
        ShowBornEffect(pos + TankBornOffset);
        yield return new WaitForSeconds(TankBornDelay);
        var unit = CreateUnit(pos, tankPrefabs, type, TankBornOffset, transParentEnemy, EDir.Down, allEnmey);
        unit.camp = Global.EnemyCamp;
        RemainEnemyCount--;
    }

    public IEnumerator YiledCreatePlayer(Vector2 pos, int type){
        ShowBornEffect(pos + TankBornOffset);
        AudioManager.PlayClipBorn();
        yield return new WaitForSeconds(TankBornDelay);
        DirectCreatePlayer(pos, type);
    }

    private Tank DirectCreatePlayer(Vector2 pos, int type){
        var unit = CreateUnit(pos, playerPrefabs, type, TankBornOffset, transParentPlayer, EDir.Up, allPlayer);
        unit.camp = Global.PlayerCamp;
        unit.brain.enabled = false;
        myPlayer = unit;
        myPlayer.name = "PlayerTank";
        RemainPlayerLife--;
        return unit;
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
        if (unit is Tank) {
            unit.size = Vector2.one;
            unit.radius = unit.size.magnitude;
        }

        if (unit is Item) {
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
                    Score += (tank.detailType + 1) * 100;
                    if (OnScoreChanged != null) {
                        OnScoreChanged(Score);
                    }

                    if ( //tank.detailType >= Global.ItemTankType &&
                        itemPrefabs.Count > 0) {
                        var x = Random.Range(min.x + 1.0f, max.x - 3.0f);
                        var y = Random.Range(min.y + 1.0f, max.y - 3.0f);
                        CreateItem(new Vector2(x, y), Random.Range(0, itemPrefabs.Count));
                    }
                }

                if (tank.camp == Global.PlayerCamp) {
                    if (RemainPlayerLife > 0) {
                        CreatePlayer(playerBornPoint, 0);
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