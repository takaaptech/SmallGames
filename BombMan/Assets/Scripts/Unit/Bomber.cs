using System.Collections.Generic;
using UnityEngine;



public class Bomber : Unit {
    public Walker owner;
    public float timeToBoom;
    public int damageRange;
    private float timer;
    public virtual void DoUpdate(float deltaTime){
    }
}
