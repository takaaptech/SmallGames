using UnityEngine;

public class ItemBoom : Item {
    protected override void OnTriggelEffect(Tank unit){
        foreach (var tank in GameManager.Instance.allEnmey) {
            tank.health = 0;
        }
    }
}