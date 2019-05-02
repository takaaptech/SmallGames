using UnityEngine;

public class ItemBoom : Item {
    public int maxCount = 3;
    protected override void OnTriggerEffect(Walker trigger){
        int killCount = 0;
        foreach (var tank in GameManager.Instance.allEnmey) {
            if (tank.health > 0) {
                tank.health = 0;
                tank.killer = trigger;
                if (++killCount >= maxCount) {
                    return;
                }
            }
        }
    }
}