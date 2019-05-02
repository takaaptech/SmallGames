using UnityEngine;

public class ItemUpgrade : Item {
    protected override void OnTriggerEffect(Walker trigger){
        GameManager.Instance.Upgrade(trigger, 1);
    }
}