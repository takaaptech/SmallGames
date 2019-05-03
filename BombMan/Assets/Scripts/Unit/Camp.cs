using System.Collections.Generic;
using UnityEngine;


public class Camp : Unit {
    public override void DoDestroy(){
        GameManager.Instance.DestroyCamp(this);
        base.DoDestroy();
    }
}