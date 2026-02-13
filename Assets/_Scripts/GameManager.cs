using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public GridManager gridManager;
    public CONSTANT.GameState CurrentState = CONSTANT.GameState.IDLE;
    private Vector2 dragStartPos;
    private Candy selectedCandy;
    private Candy targetCandy;

    void Start()
    {
        if (gridManager == null)
            gridManager = FindObjectOfType<GridManager>();

        gridManager?.Initialize();
    }

    void Update()
    {
        if (CurrentState != CONSTANT.GameState.IDLE) return;

        if (Input.GetMouseButtonDown(0))
            StartDrag();

        if (Input.GetMouseButtonUp(0))
            EndDrag();
    }

    private void StartDrag()
    {
        dragStartPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        gridManager.GetCandyFromWorldPosition(dragStartPos, out int x, out int y);
        selectedCandy = gridManager.GetCandyAt(x, y);
        Debug.Log(selectedCandy != null ? $"Selected: {selectedCandy.typeCandy}" : "No candy");
    }

    private void EndDrag()
    {
        if (selectedCandy == null) return;

        Vector2 dragEndPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 delta = dragEndPos - dragStartPos;

        if (delta.magnitude < 0.5f)
        {
            ResetSelection();
            return;
        }

        GetAdjacentCandy(delta);

        if (targetCandy != null)
            gridManager.SwapCandies(selectedCandy, targetCandy);

        ResetSelection();
    }

    private void GetAdjacentCandy(Vector2 delta)
    {
        float angle = Vector2.SignedAngle(Vector2.right, delta);

        targetCandy = angle switch
        {
            >= -45f and < 45f => selectedCandy.Right,
            >= 45f and < 135f => selectedCandy.Up,
            >= -135f and < -45f => selectedCandy.Down,
            _ => selectedCandy.Left
        };
    }

    private void ResetSelection()
    {
        selectedCandy = null;
        targetCandy = null;
        dragStartPos = Vector2.zero;
    }
}
// using Unity.Collections;
// using UnityEngine;

// class GameManager : Singleton<GameManager>
// {
//     public GridManager gridManager;
//     public Vector2 startPos;
//     public Vector2 endPos;

//     private Candy a;
//     private Candy b;
//     void Start()
//     {
//         gridManager.Init();
//     }

//     void Update()
//     {
//         //gridManager.GetCandyAt(Camera.main.ScreenToWorldPoint(Input.mousePosition), out int x, out int y);
//         if (Input.GetMouseButtonDown(0))
//         {
//             startPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
//             gridManager.GetCandyAt(startPos, out int x, out int y);
//             a = gridManager.GetCandy(x, y);
//             Debug.Log(a.typeCandy + "1");

//         }
//         if (Input.GetMouseButtonUp(0))
//         {
//             endPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

//             gridManager.GetCandyAt(startPos, out int x, out int y);

//             Vector2 delta = endPos - startPos;

//             float angle = Vector2.SignedAngle(Vector2.right, delta);

//             if (angle >= -45 && angle < 45)
//                 b = a.Right;
//             else if (angle >= 45 && angle < 135)
//                 b = a.Up;
//             else if (angle >= -135 && angle < -45)
//                 b = a.Down;
//             else
//                 b = a.Left;
//             //GetAdjacentCandy(delta);
//             if (b != null)
//                 Debug.Log(b.typeCandy + "2");

//             //swap
//             if (a != null && b != null)
//                 gridManager.SwapCandies(a, b);
//         }
//     }
//     private void GetAdjacentCandy(Vector2 delta)
//     {
//         float angle = Vector2.SignedAngle(Vector2.right, delta);

//         b = angle switch
//         {
//             >= -45f and < 45f => a.Right,
//             >= 45f and < 135f => a.Up,
//             >= -135f and < -45f => a.Down,
//             _ => a.Left
//         };
//     }
// }