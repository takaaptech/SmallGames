using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

namespace DefaultNamespace {
    public class Main : MonoBehaviour {
        public GameConfig config;

        private void Awake(){
            Debug.Assert(Global.Main == null, "Main script has already existed!!!");
            Global.Main = this;
            Global.config = config; //load config
            GameObject.DontDestroyOnLoad(gameObject);
            Init();
        }

        void Init(){
            if (config.isDebug) {
                SceneManager.LoadScene(Global.SceneGame);
            }
            else {
                SceneManager.LoadScene(Global.SceneLogin);
            }
        }

        private void Update(){
            if (Global.Game != null) {
                Global.Game.DoUpdate();
            }
        }
    }
}