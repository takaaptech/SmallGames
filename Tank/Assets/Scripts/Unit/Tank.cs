using System;
using System.Collections.Generic;
using UnityEngine;

public class Tank : Unit {
    public Skill skill = new Skill();

    public Vector2 FireOffsetPos {
        get { return GetDirVec(dir); }
    }
    public override void DoStart(){
        base.DoStart();
        skill.owner = this;
        effectProxy.PlayEffectBorn(pos);
    }

    public bool TakeDamage(Bullet bullet){
        return true;
    }

    public bool Fire(){
        if (skill.CanFire()) {
           skill.Fire();
           return true;
        }

        return false;
    }

    public override void DoUpdate(float deltaTime){
        base.DoUpdate(deltaTime);
        if (skill != null) {
            skill.DoUpdate(deltaTime);
        }
    }
}

[Serializable]
public class Skill {
    public Tank owner;
    public float CD = 0.2f;
    private float CDTimer;
    public int prefabType;

    public bool CanFire(){
        return CDTimer <= 0;
    }

    public void Fire(){
        CDTimer = CD;
        var bullet = GameManager.Instance.CreateBullet(owner.pos, owner.dir, owner.FireOffsetPos, prefabType);
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