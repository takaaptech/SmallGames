using UnityEngine;

public class ItemAddLife : Item {
    protected override void OnTriggerEffect(Walker trigger){
        var gameMgr = GameManager.Instance;
        var info = gameMgr.GetPlayerFormID(trigger.UnitID);
        if (info != null) {
            info.remainPlayerLife++;
            if (gameMgr.OnLifeCountChanged != null) {
                gameMgr.OnLifeCountChanged(info);
            }
        }
    }
}