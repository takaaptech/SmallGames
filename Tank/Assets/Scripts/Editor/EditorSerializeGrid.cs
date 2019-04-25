using System.IO;
using System.Security.Policy;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace Editor {
    
    public class EditorSerializeGrid  {
        [UnityEditor.MenuItem("Tools/SerilizeCurGrid")]
        public static void DoSomething(){
            var go = GameObject.FindObjectOfType<Grid>();
            if(go == null)
                return;
            var grid = go.GetComponent<Grid>();
            if(grid == null) return;
            var bytes = TileMapSerializer.SerializeGrid(grid,LevelManager.Tile2ID);
            var sceneName = SceneManager.GetActiveScene().name;
            if (bytes != null) {
                File.WriteAllBytes(Path.Combine(Application.dataPath,"Resources/Maps/"+sceneName+".bytes"),bytes);    
            }
            AssetDatabase.Refresh();
            Debug.Log("Done");
        }

       
    }
}