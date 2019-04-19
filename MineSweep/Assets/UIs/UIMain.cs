using DefaultNamespace;
using UnityEngine;
using UnityEngine.UI;

public class UIMain : MonoBehaviour {
    public UIGameSelect uiSelect;
    public UIGame uiGame;


    private void Start(){
        uiSelect.OnStart();
    }

    public void OnMsg_OnFinishedSlelectGame(){
        uiSelect.gameObject.SetActive(false);
        uiGame.gameObject.SetActive(true);
        uiGame.StartGame();
    }

    public void OnMsg_BackToUISelectGame(){
        uiSelect.gameObject.SetActive(true);
        uiGame.gameObject.SetActive(false);
    }
}