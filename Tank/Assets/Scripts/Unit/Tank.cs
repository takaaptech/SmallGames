using UnityEngine;

public class Unit {
    public int camp; //阵营
    public int health;
}

public class Tank :Unit{
    public float moveSpd;
    public float atkSpd;
    public int damage;
}


public class Bullet {
    public Tank owner;
    public Vector2Int moveDir;
    public float moveSpd;
    public float radius;//半径
}

public class Brick : Unit { }

public class Iron : Unit { }
public class Camp : Unit { }

public class Player : Tank { }
public class Enemy : Tank { }


public class Item {
    public int Type;
    public float lifeTime;
    public BoundsInt bound;
}