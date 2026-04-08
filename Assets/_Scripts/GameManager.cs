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
    private Camera mainCamera;
    private bool isAutoPlayEnabled;
    private float nextAutoPlayTime;
    private float lastPlayerInputTime;
    private float nextHintTime;
    private int failedSwapCount;

    protected override void Awake()
    {
        base.Awake();
        mainCamera = Camera.main;
        if (gridManager == null)
            gridManager = GetComponent<GridManager>();
        if (gridManager == null)
            gridManager = FindObjectOfType<GridManager>();
    }

    void Start()
    {
        gridManager?.Initialize();
        lastPlayerInputTime = Time.time;
        nextHintTime = Time.time + Constants.HINT_IDLE_DELAY_SECONDS;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            ToggleAutoPlay();
        }

        if (CurrentState != Constants.GameState.IDLE) return;

        if (isAutoPlayEnabled)
        {
            AutoPlayStep();
            return;
        }

        TryShowIdleHint();

        if (Input.GetMouseButtonDown(0))
            StartDrag();

        if (Input.GetMouseButtonUp(0))
            EndDrag();
    }

    private void ToggleAutoPlay()
    {
        isAutoPlayEnabled = !isAutoPlayEnabled;
        nextAutoPlayTime = Time.time;
        ResetSelection();
        failedSwapCount = 0;
        lastPlayerInputTime = Time.time;
        nextHintTime = Time.time + Constants.HINT_IDLE_DELAY_SECONDS;
        Debug.Log(isAutoPlayEnabled ? "Auto Play: ON" : "Auto Play: OFF");
    }

    private void AutoPlayStep()
    {
        if (gridManager == null) return;
        if (Time.time < nextAutoPlayTime) return;

        if (gridManager.TryFindBestValidMove(out Candy first, out Candy second) ||
            gridManager.TryFindAnyValidMove(out first, out second))
        {
            gridManager.ShowHint(first, second);
            gridManager.SwapCandies(first, second);
        }
        else
        {
            gridManager.ShuffleBoardKeepSpecials();
            Debug.Log("Auto Play: no valid moves, shuffled board.");
        }

        nextAutoPlayTime = Time.time + Constants.AUTO_PLAY_STEP_DELAY;
    }

    private void StartDrag()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        lastPlayerInputTime = Time.time;

        dragStartPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        gridManager.GetCandyPositionFromWorldPosition(dragStartPos, out int x, out int y);
        selectedCandy = gridManager.GetCandyAt(x, y);
    }

    private void EndDrag()
    {
        if (selectedCandy == null) return;

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null)
        {
            ResetSelection();
            return;
        }

        Vector2 dragEndPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 delta = dragEndPos - dragStartPos;

        if (delta.magnitude < 0.5f)
        {
            ResetSelection();
            return;
        }

        GetAdjacentCandy(delta);

        if (targetCandy != null)
        {
            bool willResolve = gridManager.WillSwapResolve(selectedCandy, targetCandy);
            gridManager.SwapCandies(selectedCandy, targetCandy);

            if (willResolve)
            {
                failedSwapCount = 0;
                nextHintTime = Time.time + Constants.HINT_IDLE_DELAY_SECONDS;
            }
            else
            {
                failedSwapCount++;
            }

            lastPlayerInputTime = Time.time;
        }

        ResetSelection();
    }

    private void TryShowIdleHint()
    {
        if (gridManager == null) return;
        if (Time.time < nextHintTime) return;

        bool isIdleTooLong = (Time.time - lastPlayerInputTime) >= Constants.HINT_IDLE_DELAY_SECONDS;
        bool hasFailedTooMany = failedSwapCount >= Constants.HINT_FAILED_SWAP_THRESHOLD;
        if (!isIdleTooLong && !hasFailedTooMany) return;

        if (gridManager.TryFindBestValidMove(out Candy first, out Candy second) ||
            gridManager.TryFindAnyValidMove(out first, out second))
        {
            gridManager.ShowHint(first, second);
        }

        nextHintTime = Time.time + Constants.HINT_REPEAT_COOLDOWN_SECONDS;
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
