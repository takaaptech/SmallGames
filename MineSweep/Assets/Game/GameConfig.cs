using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace DefaultNamespace {

    [CreateAssetMenu(menuName="Game/Create GameConfig ")]
    public class GameConfig : ScriptableObject {
        [System.Serializable]
        public class GameDiffState {
            public int row;
            public int col;
            public float rate;
            public override string ToString(){
                return string.Format("row:{0} col:{1} rate:{2}", row, col, rate);
            }
        }
        public List<GameDiffState> allSize = new List<GameDiffState>();
        public bool isDebug = false;
    }
}