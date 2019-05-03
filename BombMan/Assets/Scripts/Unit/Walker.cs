using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Walker : Unit {
    public Skill skill = new Skill();
    public AIProxy brain = new AIProxy();
    public int killerID;

    public Vector2 FireOffsetPos {
        get { return CollisionHelper.GetDirVec(dir); }
    }

    public override void DoStart(){
        base.DoStart();
        skill.owner = this;
        brain.owner = this;
    }

    public bool TakeDamage(Bomb bomb){
        if (bomb.health >= health) {
            bomb.health -= health;
            this.health = 0;
            killerID = bomb.owner.UnitID;
            return true;
        }

        health -= bomb.health;
        bomb.health = 0;
        return false;
    }

    public bool Fire(){
        if (skill.CanFire()) {
            skill.Fire();
            return true;
        }

        return false;
    }

    public override void DoUpdate(float deltaTime){
        //update position
        var deg = CollisionHelper.GetDirDeg(dir);
        transform.rotation = Quaternion.Euler(0, 0, deg);
        //can move 判定
        var dirVec = CollisionHelper.GetDirVec(dir);
        var moveDist = (moveSpd * deltaTime);
        var fTargetHead = pos + (TANK_HALF_LEN + moveDist) * (Vector2) dirVec;
        var fPreviewHead = pos + (TANK_HALF_LEN + FORWARD_HEAD_DIST) * (Vector2) dirVec;

        float maxMoveDist = moveSpd * deltaTime;
        var headPos = pos + (TANK_HALF_LEN) * (Vector2) dirVec;
        var dist = CollisionHelper.GetMaxMoveDist(dir, headPos, fTargetHead);
        var dist2 = CollisionHelper.GetMaxMoveDist(dir, headPos, fPreviewHead);
        maxMoveDist = Mathf.Min(maxMoveDist, dist, dist2);

        var diffPos = maxMoveDist * (Vector2) dirVec;
        pos = pos + diffPos;
        if (camp == Global.PlayerCamp) {
            if (isChangedDir) {
                var idir = (int) (dir);
                var isUD = idir % 2 == 0;
                if (isUD) {
                    pos.x = CollisionHelper.RoundIfNear(pos.x - 0.5f, SNAP_DIST) + 0.5f;
                }
                else {
                    pos.y = CollisionHelper.RoundIfNear(pos.y - 0.5f, SNAP_DIST) + 0.5f;
                }
            }
        }


        transform.localPosition = pos;
        isChangedDir = false;

        if (brain.enabled) {
            brain.DoUpdate(deltaTime);
        }

        if (skill != null) {
            skill.DoUpdate(deltaTime);
        }
    }

    public override void DoDestroy(){
        var gameMgr = GameManager.Instance;
        var tank = this;
        if (tank != null) {
            if (tank.camp == Global.EnemyCamp) {
                gameMgr.DestroyEnemy(tank);
            }

            if (tank.camp == Global.PlayerCamp) {
                gameMgr.DestroyPlayer(tank);
            }
        }

        base.DoDestroy();
    }


    public Vector2 GetHeadPos(float len = FORWARD_HEAD_DIST){
        var dirVec = CollisionHelper.GetDirVec(dir);
        var fTargetHead = pos + (TANK_HALF_LEN + len) * (Vector2) dirVec;
        return fTargetHead;
    }

    private void OnDrawGizmos(){
#if UNITY_EDITOR
        if (!Application.isPlaying) return;
        var debugInfo = CollisionHelper.DebugQueryCollider(dir, GetHeadPos(FORWARD_HEAD_DIST));
        //foreach (var info in debugInfo) {
        //    if (info.z == 1) {
        //        Gizmos.DrawWireCube(info + transform.parent.position + new Vector3(0.5f, 0.5f, 0), Vector3.one * 0.5f);
        //    }
        //    else {
        //        Gizmos.DrawWireSphere(info + transform.parent.position + new Vector3(0.5f, 0.5f, 0), 0.5f);
        //    }
        //}

        debugInfo.Clear();
#endif
    }
}

[Serializable]
public class AIProxy {
    public Walker owner;
    private float timer;
    public float updateInterval = 1;
    public float fireRate = 0.3f;
    public bool enabled = true;
    /// <summary>
    /// the rate to changed dir if it can
    /// </summary>
    public float turnRate = 0.2f;
    private const float TargetDist = 0.1f;
    public const float sqrtTargetDist = TargetDist * TargetDist;

    private Vector2Int TargetPos = new Vector2Int(-1000,-1000);
    private EDir TargetDir = EDir.Up;
    
    public void DoUpdate(float deltaTime){
        timer += deltaTime;
        if (timer < updateInterval) {
            return;
        }

        if (owner == null)
            return;
        timer = 0;
        Vector2Int dir = Vector2Int.zero;
        var isReachTheEnd = CollisionHelper.HasColliderWithBorder(owner.dir, owner.GetHeadPos());
        if (isReachTheEnd) {
            List<int> allWalkableDir = new List<int>();
            for (int i = 0; i < (int) (EDir.EnumCount); i++) {
                var vec = (Vector2) CollisionHelper.GetDirVec((EDir) i) * Walker.TANK_HALF_LEN;
                var pos = owner.pos + vec;
                if (!CollisionHelper.HasCollider(pos)) {
                    allWalkableDir.Add(i);
                }
            }

            var count = allWalkableDir.Count;
            if (count > 0) {
                owner.dir = (EDir) (allWalkableDir[Random.Range(0, count)]);
            }

            return;
        }

        var iPos = owner.pos.Floor();
        if ((owner.pos - (iPos + Global.UnitSizeVec)).sqrMagnitude < sqrtTargetDist) {
            if (Random.value > turnRate) {
                return;
            }
            //random change dir if it can 
            var borderDir = CollisionHelper.GetDirVec((EDir) ((int) (owner.dir + 1) % (int) EDir.EnumCount));
            var iBorder1 = iPos + borderDir;
            var iBorder2 = iPos - borderDir;
            if (!CollisionHelper.HasCollider(iBorder1)) {
                owner.dir = CollisionHelper.GetEDirFromVec(borderDir);
            }
            else if (!CollisionHelper.HasCollider(iBorder2)) {
                owner.dir = CollisionHelper.GetEDirFromVec(Vector2Int.zero -borderDir);
            }
        }
        
    }

}

[Serializable]
public class Skill {
    public Walker owner;
    public float CD = 0.2f;
    private float CDTimer;
    public int prefabType;

    public bool CanFire(){
        if (CDTimer > 0)
            return false;
        //check targetTile is air
        var ownPos = owner.pos.Floor() + CollisionHelper.GetDirVec(owner.dir);
        var id = LevelManager.Instance.Pos2TileID(ownPos, true);
        return id == 0;
    }

    public void Fire(){
        CDTimer = CD;
        var bullet = GameManager.Instance.CreateBomb(owner.pos.Floor() + Global.UnitSizeVec, owner.dir,
            owner.FireOffsetPos, prefabType);
        bullet.owner = owner;
        bullet.camp = owner.camp;
    }

    public void DoUpdate(float deltaTime){
        CDTimer -= deltaTime;
        if (CDTimer < 0) {
            CDTimer = 0;
        }
    }
}