using DefaultNamespace;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public class UIGame : MonoBehaviour {
    public int rowCount {
        get { return Global.Game.row; }
    }
    public int colCount{
        get { return Global.Game.col; }
    }

    public GameObject prefab;
    public GameObject resultPanel;
    
    public GridLayoutGroup layoutGroup;

    public Text txtTimer;
    public Toggle toggleIsTargetMine;
    
    private int timer = -1;
    // Start is called before the first frame update
    
    public  void StartGame(){
        Debug.Log("StartGame");
        if (prefab == null)
            return;
        prefab.SetActive(true);
        for (int i = 0; i < rowCount; i++) {
            for (int j = 0; j < colCount; j++) {
                CreateButton(new Vector2Int(i,j));
            }
        }
        prefab.SetActive(false);
        Global.Game.OnFinishCreateCells();
        toggleIsTargetMine.onValueChanged.RemoveAllListeners();
        toggleIsTargetMine.onValueChanged.AddListener((isOn) => { Global.Game.isFlagMode = isOn; });
    }

    void CreateButton(Vector2Int pos){
        var go = GameObject.Instantiate(prefab, layoutGroup.transform, false);
        go.name ="Cell " +  pos.ToString();
        var cell = go.GetComponent<Cell>();
        Global.Game.OnCreateCell(cell,pos);
    }

    private void Update(){
        if (Global.Game!= null && timer != Global.Game.timer) {
            txtTimer.text = timer.ToString();
            timer = Global.Game.timer;
        }
    }
}