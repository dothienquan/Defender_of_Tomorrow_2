using UnityEngine;

public enum Direction { Up = 0, Right = 1, Down = 2, Left = 3 }

public static class DirectionUtils
{
    public static Direction Rotate90(this Direction d)
    {
        return (Direction)(((int)d + 1) % 4);
    }

    public static Vector2 ToVector(this Direction d)
    {
        switch (d)
        {
            case Direction.Up: return Vector2.up;
            case Direction.Right: return Vector2.right;
            case Direction.Down: return Vector2.down;
            case Direction.Left: return Vector2.left;
        }
        return Vector2.right;
    }

    public static float ToAngleZ(this Direction d)
    {
        switch (d)
        {
            case Direction.Right: return 0f;
            case Direction.Up: return 90f;
            case Direction.Left: return 180f;
            case Direction.Down: return 270f;
        }
        return 0f;
    }
}
