using System.Collections.Generic;
using UnityEngine;



public class Bullet : Unit {
    public Tank owner;

    public virtual void DoUpdate(float deltaTime){
        //update position
        Vector2 dirVec = GetDirVec(dir);
        var offset = (moveSpd * deltaTime) * dirVec;
        pos += offset;
        transform.localPosition = pos;
    }
}
