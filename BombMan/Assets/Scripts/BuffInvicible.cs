using UnityEngine;

public class BuffInvicible : MonoBehaviour {
    public float duration = 3;
    public float timer;
    private bool hasAttached = false;
    private Unit unit;
    public Color rawColor;

    public void OnAttach(){
        if (hasAttached) {
            return;
        }

        hasAttached = true;

        unit = GetComponent<Unit>();
        if (unit != null) {
            unit.isInvicible = true;
            var buffs = GetComponents<BuffInvicible>();
            if (buffs.Length != 1) {
                foreach (var buff in buffs) {
                    if (buff != this) {
                        rawColor = buff.rawColor;
                    }
                }
            }
            else {
                rawColor = unit.GetComponentInChildren<SpriteRenderer>().color;
            }

            unit.GetComponentInChildren<SpriteRenderer>().color = Color.cyan;
        }
    }

    public void OnRemove(){
        if (unit != null) {
            unit.isInvicible = false;
            unit.GetComponentInChildren<SpriteRenderer>().color = rawColor;
        }
    }

    private void Update(){
        if (!hasAttached) {
            OnAttach();    
        }

        timer += Time.deltaTime;
        if (timer >= duration) {
            OnRemove();
            GameObject.Destroy(this);
        }
    }
}