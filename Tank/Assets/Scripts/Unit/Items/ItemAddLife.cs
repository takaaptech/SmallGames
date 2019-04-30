using UnityEngine;

public class ItemAddLife : Item {
    protected override void OnTriggelEffect(Tank unit){
        if (unit == GameManager.Instance.myPlayer) {
            GameManager.Instance.RemainPlayerLife++;
        }
    }
}