using System;
using UnityEngine;

public static class CoordinateSystems
{
    public static class Cartesian2D{
        public static Vector2Int LEFT = new Vector2Int(-1,0);
        public static Vector2Int RIGHT = new Vector2Int(1,0);
        public static Vector2Int UP = new Vector2Int(0,-1);
        public static Vector2Int DOWN = new Vector2Int(0,1);
    }   

    public static class Hex{
        public static Vector2Int UP_RIGHT = new Vector2Int(1,-1);
        public static Vector2Int RIGHT = new Vector2Int(1,0);
        public static Vector2Int DOWN_RIGHT = new Vector2Int(0,1);
        public static Vector2Int DOWN_LEFT = new Vector2Int(-1,1);
        public static Vector2Int LEFT = new Vector2Int(-1,0);
        public static Vector2Int UP_LEFT = new Vector2Int(0,-1);

        public static void GetCenterPosition(Vector2Int coord){
            int q = coord.x;
            int r = coord.y;
            float cx = q * Mathf.Sqrt(3) + r * Mathf.Sqrt(3) / 2;
            float cz = -r * 1.5f;
        }
    }
}
