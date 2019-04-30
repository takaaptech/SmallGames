using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class UIManager : BaseManager<UIManager> {
    public Text txtCurLevel;
    public Text txtScore;
    public Text txtRemainEnemy;
    public Text txtRemainLife;
    public Text txtMessage;

    public override void DoStart(){
        base.DoStart();
        var gMgr = GameManager.Instance;
        gMgr.OnLevelChanged += OnLevelChanged;
        gMgr.OnScoreChanged += OnScoreChanged;
        gMgr.OnLifeCountChanged += OnLifeChanged;
        gMgr.OnEnmeyCountChanged += OnEnmeyCountChanged;
        gMgr.OnMessage += OnMessageStr;
    }

    void OnMessageStr(string msg){
        txtMessage.text = msg;
    }

    void OnLevelChanged(int val){
        txtCurLevel.text = val.ToString();
    }

    void OnScoreChanged(int val){
        txtScore.text = val.ToString();
    }

    void OnLifeChanged(int val){
        txtRemainLife.text = val.ToString();
    }

    void OnEnmeyCountChanged(int val){
        txtRemainEnemy.text = val.ToString();
    }
}