using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace DefaultNamespace {
    public class UIGameSelect : MonoBehaviour {
        public Dropdown dropdown;
        public Button btnStartGame;
        public UIMain uiMain;
        public void OnStart(){
            if (btnStartGame == null) {
                return;
            }

            dropdown.ClearOptions();
            List<string> allOpetions = new List<string>();
            foreach (var size in Global.config.allSize) {
                allOpetions.Add(size.ToString());
            }

            dropdown.AddOptions(allOpetions);
            dropdown.onValueChanged.AddListener((idx) => { this.selectIdx = idx; });
            btnStartGame.onClick.AddListener(OnClick_BtnStartGame);
        }

        [SerializeField] private int selectIdx;

        void OnClick_BtnStartGame(){
            var size = Global.config.allSize[selectIdx];
            Global.SetGame(new Game());
            Global.Game.row = size.row;
            Global.Game.col = size.col;
            Global.Game.rate = size.rate;
            uiMain.OnMsg_OnFinishedSlelectGame();
        }
    }
}