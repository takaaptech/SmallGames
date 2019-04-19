namespace DefaultNamespace {
    /// <summary>
    /// 玩家属性
    /// </summary>
    public class Player {
        /// <summary>
        /// 玩家名字
        /// </summary>
        public string name;
        /// <summary>
        /// 玩家ID
        /// </summary>
        public long userID;
        /// <summary>
        /// 经验
        /// </summary>
        public int exp;
        /// <summary>
        /// 玩家等级
        /// </summary>
        public int level;
        /// <summary>
        /// 金币  游戏币  
        /// </summary>
        public int coin;
        /// <summary>
        /// 钻石数量  需要rmb 充值
        /// </summary>
        public int diamond;
        /// <summary>
        /// 受到限制的钻石 礼包赠送 
        /// </summary>
        public int limitedDiamond;
    }
}