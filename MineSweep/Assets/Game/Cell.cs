using System.IO.IsolatedStorage;
using UnityEngine;
using UnityEngine.Experimental.Audio.Google;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace DefaultNamespace {
    public enum ECellType {
        /// <summary>
        /// 未点开
        /// </summary>
        UnOpened,

        /// <summary>
        /// 数字
        /// </summary>
        Number,

        /// <summary>
        /// 地雷标记
        /// </summary>
        Flag,

        /// <summary>
        /// 地雷
        /// </summary>
        Mine,
    }

    public class Cell : MonoBehaviour {
        public Button btn;
        public Text txtCount;
        public Image imgFloat;
        public Image imgFlag;
        public Image imgMine;

        public ECellType statue = ECellType.UnOpened;
        public Vector2Int pos;
        public int mineCountInBorder;

        public void Reset(){
            SetState(ECellType.UnOpened, 0);
        }

        public void SetState(int mineCount){
            SetStatus(false, true, false, false, mineCount == 0 ? "" : mineCount.ToString());
        }

        public void SetState(ECellType statue, int mineCount = 0){
            if ((statue != ECellType.Number && this.statue == statue)
                || (statue == ECellType.Number && mineCountInBorder == mineCount)) {
                //is the same do not need reflash
                return;
            }

            this.statue = statue;
            switch (statue) {
                case ECellType.UnOpened: {
                    SetStatus(false, false, true, false, "");
                    break;
                }
                case ECellType.Number: {
                    SetStatus(false, true, false, false, mineCount == 0 ? "" : mineCount.ToString());
                    break;
                }
                case ECellType.Flag: {
                    SetStatus(true, false, true, false, "");
                    break;
                }
                case ECellType.Mine: {
                    SetStatus(false, false, false, true, "");
                    break;
                }
            }
        }

        void SetStatus(bool isFlag, bool isTxt, bool isBtn, bool isMine, string text){
            if (txtCount.enabled != isTxt) txtCount.enabled = isTxt;
            if (isTxt) txtCount.text = text;
            if (imgFlag.enabled != isFlag) imgFlag.enabled = isFlag;
            if (imgMine.enabled != isMine) imgMine.enabled = isMine;
            if (btn.enabled != isBtn) {
                btn.enabled = isBtn;
                imgFloat.enabled = isBtn;
            }
        }

    }
}