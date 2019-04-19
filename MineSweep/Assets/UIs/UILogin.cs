using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DefaultNamespace {

    public class UILogin : MonoBehaviour {
        public InputField account;
        public InputField pwd;

        public Button BtnLogin;

        private void Start(){
            if (BtnLogin == null) {
                BtnLogin = transform.Find("BtnLogin")?.GetComponent<Button>();
            }

            Debug.Assert(BtnLogin != null, "Miss login button");
            BtnLogin.onClick.AddListener(OnClick_BtnLogin);
        }

        void OnClick_BtnLogin(){
            //上传验证
            var strAccount = account.text;
            var strPwd = pwd.text;
            SendMsg(strAccount, strPwd);
        }


        public class LoginResult {
            public string account;
            public string pwd;
            public bool isSucc = true;

            public string name {
                get { return account; }
            }
        }
        void SendMsg(string account, string pwd){
            OnMsg_LoginResult(new LoginResult {account = account, pwd = pwd});
        }

        void OnMsg_LoginResult(System.Object infoObj){
            var result = infoObj as LoginResult;
            if (result != null) {
                if (!result.isSucc) {
                    Debug.LogError("Login failed");
                    return;
                }
                var player = new Player();
                player.name = result.name;
                Global.SetPlayer(player);
                Debug.Log("LoginSucc");
                SceneManager.LoadScene(Global.SceneGame);
            }
        }
    }
}