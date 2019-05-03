using System.Collections.Generic;
using UnityEngine;


public class Unit : MonoBehaviour {
    /// <summary> 阵营</summary>
    public int camp; //阵营

    public int detailType;
    public int UnitID;
    [Header("Move Infos")] public int health = 1;
    public float moveSpd = 2;
    public float MaxMoveSpd = 2;
    public EDir _dir;
    public bool isInvicible = false;
    public EDir dir {
        get { return _dir; }
        set {
            if (_dir != value) {
                isChangedDir = true;
            }

            _dir = value;
        }
    }

    protected bool isChangedDir = false;

    [Header("Collision")]
    //for collision
    public Vector2 pos; //center

    public Vector2 size; //for aabb
    public float radius; //for circle

    //const vals
    public const float TANK_HALF_LEN = Global.UnitSize;
    public const float FORWARD_HEAD_DIST = Global.UnitSize * 0.05f;
    public const float SNAP_DIST = Global.UnitSize * 0.6f;

    public virtual bool CanMove(){
        return true;
    }

    public virtual void DoStart(){ }


    public virtual void DoUpdate(float deltaTime){ }


    public virtual void DoDestroy(){
        GameObject.Destroy(gameObject);
    }

    #region  debug infos

    #endregion
}