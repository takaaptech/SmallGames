using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

[System.Serializable]
public class GameManager : BaseManager<GameManager> {
    public Transform transParentPlayer;
    public Transform transParentEnemy;
    public Transform transParentItem;
    public Transform transParentBullet;
    public int CurLevel = 0;

    public override void DoAwake(){
        CurLevel = PlayerPrefs.GetInt("GameLevel", 0);
        transParentPlayer = CreateChildTrans("Players");
        transParentEnemy = CreateChildTrans("Enemies");
        transParentItem = CreateChildTrans("Items");
        transParentBullet = CreateChildTrans("Bullets");
    }

    public Tank myPlayer;
    public List<Tank> allEnmey = new List<Tank>();
    public List<Tank> allPlayer = new List<Tank>();
    public List<Bullet> allBullet = new List<Bullet>();
    public List<Item> allItem = new List<Item>();

    public Camp camp;
    public List<Vector2Int> enemyBornPoints = new List<Vector2Int>();
    public Vector2Int playerBornPoint;
    public Vector2Int player2BornPoint;

    //大本营
    public BoundsInt campBound;

    public List<GameObject> tankPrefabs = new List<GameObject>();
    public List<GameObject> bulletPrefabs = new List<GameObject>();
    public List<GameObject> itemPrefabs = new List<GameObject>();
    public GameObject CampPrefab;

    public const int EnemyCamp = 1;
    public const int PlayerCamp = 2;

    public Tank CreateEnemy(Vector2Int pos, int type){
        var unit = CreateUnit(pos, tankPrefabs, type, Vector2.one, transParentEnemy, EDir.Down, allEnmey);
        unit.camp = EnemyCamp;
        return unit;
    }

    public Tank CreatePlayer(Vector2Int pos, int type){
        var unit = CreateUnit(pos, tankPrefabs, type, Vector2.one, transParentPlayer, EDir.Up, allPlayer);
        unit.camp = PlayerCamp;
        return unit;
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

    public void DestroyUnit<T>(T unit, List<T> lst) where T : Unit{
        if (lst.Remove(unit)) {
            unit.DoDestroy();
        }
    }

    public Dictionary<Vector2Int, LinkedList<Bullet>>
        pos2EnmeyBullet = new Dictionary<Vector2Int, LinkedList<Bullet>>();

    public Dictionary<Vector2Int, LinkedList<Bullet>> pos2PlayerBullet =
        new Dictionary<Vector2Int, LinkedList<Bullet>>();

    public int maxEnemyCount = 6;
    public int initEnemyCount = 20;
    public int remainEnemyCount = 20;

    private void ColliderDetected(){
        // update Bounding box

        HashSet<Unit> tempLst = new HashSet<Unit>();
        // bullet and tank
        foreach (var bullet in allBullet) {
            var bulletCamp = bullet.camp;
            foreach (var tank in allPlayer) {
                if (tank.camp != bulletCamp && IsCollided(bullet, tank)) {
                    tempLst.Add(bullet);
                    if (tank.TakeDamage(bullet)) {
                        tempLst.Add(tank);
                    }
                }
            }

            foreach (var tank in allEnmey) {
                if (tank.camp != bulletCamp && IsCollided(bullet, tank)) {
                    tempLst.Add(bullet);
                    if (tank.TakeDamage(bullet)) {
                        tempLst.Add(tank);
                    }
                }
            }
        }

        // bullet and camp
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

        // tank   and item

        foreach (var unit in tempLst) {
            GameManager.Instance.DestroyUnit(unit as Bullet, GameManager.Instance.allBullet);
            GameManager.Instance.DestroyUnit(unit as Tank, GameManager.Instance.allPlayer);
            GameManager.Instance.DestroyUnit(unit as Tank, GameManager.Instance.allEnmey);
        }
    }

    void CheckBulletWithMap(Vector2 pos, HashSet<Unit> tempLst, Unit bullet){
        var id = LevelManager.Instance.Pos2TileID(pos, true);
        if (id != 0) {
            //collide bullet with world
            if (id == Global.TileID_Brick) {
                LevelManager.Instance.ReplaceTile(pos, id, 0);
                tempLst.Add(bullet);
            }

            if (id == Global.TileID_Iron || id == Global.TileID_Wall) {
                tempLst.Add(bullet);
            }
        }
    }

    public Vector2Int min;
    public Vector2Int max;

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

    /// <summary>
    /// 正式开始游戏
    /// </summary>
    public void StartGame(){
        // 
        var tileInfo = main.levelMgr.GetMapInfo(Global.TileMapName_BornPos);
        var campPoss = tileInfo.GetAllTiles(LevelManager.ID2Tile(Global.TileID_Camp));
        Debug.Assert(campPoss != null && campPoss.Count == 1, "campPoss!= null&& campPoss.Count == 1");
        enemyBornPoints = tileInfo.GetAllTiles(LevelManager.ID2Tile(Global.TileID_BornPosEnemy));
        var heroBornPoss = tileInfo.GetAllTiles(LevelManager.ID2Tile(Global.TileID_BornPosHero));
        if (heroBornPoss.Count > 1) {
            playerBornPoint = heroBornPoss[0];
            player2BornPoint = heroBornPoss[1];
        }


        //create palyers
        myPlayer = CreatePlayer(playerBornPoint, 0);
        myPlayer.name = "PlayerTank";
        //create camps
        var pos = (campPoss[0] + Vector2.one);
        camp = GameObject.Instantiate(CampPrefab, pos, Quaternion.identity, transParentItem.parent)
            .GetComponent<Camp>();
        camp.pos = pos;
        camp.size = Vector2.one;
    }


    public override void DoUpdate(float deltaTime){
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
        if (allEnmey.Count < maxEnemyCount && remainEnemyCount > 0) {
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

    public float bornEnemyInterval = 3;
    public float bornTimer;
}