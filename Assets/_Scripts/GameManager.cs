using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public GridManager gridManager;
    public Constants.GameState CurrentState = Constants.GameState.IDLE;
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
        if (CurrentState != Constants.GameState.IDLE) return;

        if (Input.GetMouseButtonDown(0))
            StartDrag();

        if (Input.GetMouseButtonUp(0))
            EndDrag();
    }

    private void StartDrag()
    {
        dragStartPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        gridManager.GetCandyPositionFromWorldPosition(dragStartPos, out int x, out int y);
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
