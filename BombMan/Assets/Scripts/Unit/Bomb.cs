using System.Collections.Generic;
using UnityEngine;


public class Bomb : Unit {
    public Walker owner;
    public float timeToBoom;
    public int damageRange;
    private float timer;
    public bool isExploded = false;

    public override void DoUpdate(float deltaTime){
        if (isExploded) {
            return;
        }

        timer += deltaTime;
        if (timer > timeToBoom) {
            isExploded = true;
        }
    }

    public override void DoDestroy(){
        GameManager.Instance.DestroyBomb(this);
        base.DoDestroy();
    }
}