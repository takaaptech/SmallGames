using System.Collections.Generic;
using UnityEngine;


public class Unit : MonoBehaviour {
    /// <summary> 阵营</summary>
    public int camp; //阵营

    public int health = 100;
    public float moveSpd = 2;
    public float maxMoveSpd = 2;
    public EDir dir;

    //for collision
    public Vector2 pos; //center
    public Vector2 size; //for aabb
    public float radius; //for circle

    public EffectProxy effectProxy = new EffectProxy();

    public static Vector2Int GetDirVec(EDir dir){
        switch (dir) {
            case EDir.Up: return Vector2Int.up;
            case EDir.Right: return Vector2Int.right;
            case EDir.Down: return Vector2Int.down;
            case EDir.Left: return Vector2Int.left;
        }

        return Vector2Int.up;
    }

    public static int GetDirDeg(EDir dir){
        return ((int) dir) * 90;
    }

    public virtual bool CanMove(){
        return true;
    }

    public virtual void DoStart(){
        effectProxy.PlayEffectBorn(pos);
    }

    private const int TankRadius = 1;
    private const float PreviewHeadRadius = 0.05f;

    float GetMaxMoveDist(Vector2 fHeadPos, Vector2 fTargetHead, float maxMoveDist, Vector2 borderDir){
        var iTargetHead = new Vector2Int(Mathf.FloorToInt(fTargetHead.x), Mathf.FloorToInt(fTargetHead.y));
        var fBorder1 = fTargetHead + borderDir * 0.9f;
        var fBorder2 = fTargetHead - borderDir * 0.9f;
        var isColHead = IsCollider(fTargetHead);
        var isColBorder1 = IsCollider(fBorder1);
        var isColBorder2 = IsCollider(fBorder2);


        if (isColHead
            || isColBorder1
            || isColBorder2
        ) {
            switch (dir) {
                case EDir.Up:
                    maxMoveDist = iTargetHead.y - fHeadPos.y;
                    break;
                case EDir.Right:
                    maxMoveDist = iTargetHead.x - fHeadPos.x;
                    break;
                case EDir.Down:
                    maxMoveDist = fHeadPos.y - iTargetHead.y - 1;
                    break;
                case EDir.Left:
                    maxMoveDist = fHeadPos.x - iTargetHead.x - 1;
                    break;
            }
        }

        return maxMoveDist;
    }

    public static Vector2Int GetBorderDir(EDir dir){
        var isUpDown = (int) (dir) % 2 == 0;
        var borderDir = Vector2Int.up;
        if (isUpDown) {
            borderDir = Vector2Int.right;
        }

        return borderDir;
    }
    public virtual void DoUpdate(float deltaTime){
        //update position
        var dirVec = GetDirVec(dir);
        var moveDist = (moveSpd * deltaTime);
        var deg = GetDirDeg(dir);
        transform.rotation = Quaternion.Euler(0, 0, deg);
        //can move 判定
        //
        var fTargetHead = pos + (TankRadius + moveDist) * (Vector2) dirVec;
        var fPreviewHead = pos + (TankRadius + PreviewHeadRadius) * (Vector2) dirVec;

        var borderDir = GetBorderDir(dir);
        float maxMoveDist = moveSpd * deltaTime;
        var headPos = pos + (TankRadius) * (Vector2) dirVec;
        var dist = GetMaxMoveDist(headPos, fTargetHead, maxMoveDist, borderDir);
        var dist2 = GetMaxMoveDist(headPos, fPreviewHead, maxMoveDist, borderDir);
        maxMoveDist = Mathf.Min(maxMoveDist, dist, dist2);
        var diffPos = maxMoveDist * (Vector2) dirVec;
        pos = pos + diffPos;
        transform.localPosition = pos;
    }

    bool IsCollider(Vector2 pos){
        var iPos = new Vector2Int(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y));
        var id = LevelManager.Instance.Pos2TileID(iPos, true);

        if (this.name.Contains("Player")) {
            DrawDebugView(iPos, id != 0);
        }

        return id != 0;
    }

    public void DoDestroy(){
        effectProxy.PlayEffectDestory(pos);
        GameObject.Destroy(gameObject);
    }

    private List<Vector3Int> debugInfo = new List<Vector3Int>();

    void DrawDebugView(Vector2Int pos, bool isCollider){
        debugInfo.Add(new Vector3Int(pos.x, pos.y, isCollider ? 1 : 0));
    }

    private void OnDrawGizmos(){
        foreach (var info in debugInfo) {
            if (info.z == 1) {
                Gizmos.DrawWireCube(info + transform.parent.position + new Vector3(0.5f, 0.5f, 0), Vector3.one * 0.5f);
            }
            else {
                Gizmos.DrawWireSphere(info + transform.parent.position + new Vector3(0.5f, 0.5f, 0), 0.5f);
            }
        }

        debugInfo.Clear();
    }
}