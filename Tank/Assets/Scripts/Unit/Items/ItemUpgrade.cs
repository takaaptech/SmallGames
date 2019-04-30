using UnityEngine;

public class ItemUpgrade : Item {
    protected override void OnTriggelEffect(Tank unit){
        GameManager.Instance.Upgrade(unit, 1);
    }
}