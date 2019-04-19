namespace DefaultNamespace {
    public static class Global {
        //const define
        public static string SceneGame = "Game";
        public static string SceneLogin = "Login";


        public static GameConfig config;
        //global variables 
        public static int GameType { get; set; }
        public static int SmallGameType{ get; set; }
        public static Player Player { get; private set; }
        public static Game Game { get; private set; }
        public static Main Main { get; set; }

        public static void SetPlayer(Player player){
            Player = player;
        }
        public static void SetGame(Game game){
            Game = game;
        }
    }
}