using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private GameObject[] prefabCandies;
    [SerializeField] private GameObject[] prefabFxs;

    public Candy[,] candies;
    private int width;
    private int height;

    public int Width => width;
    public int Height => height;

    private bool isProcessing = false;

    public void Initialize()
    {
        width = CONSTANT.WIDTH;
        height = CONSTANT.HEIGHT;
        candies = new Candy[width, height];

        PositionCamera();
        InitializeGrid();
        RefillGridUntilNoMatches();
    }

    private void PositionCamera()
    {
        Camera.main.transform.position = new Vector3((width - 1) * CONSTANT.TILE_SIZE / 2f, (height - 1) * CONSTANT.TILE_SIZE / 2f, -10);
    }

    private void InitializeGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                SpawnNewCandyAt(x, y);
            }
        }
    }

    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    public Candy GetCandyAt(int x, int y)
    {
        if (!IsValidPosition(x, y))
            return null;

        return candies[x, y];
    }

    private void SpawnNewCandyAt(int x, int y)
    {
        if (!IsValidPosition(x, y))
            return;

        RemoveCandyAt(x, y);

        int randomIndex = Random.Range(0, prefabCandies.Length);
        GameObject candyObj = Instantiate(prefabCandies[randomIndex]);

        if (candyObj == null)
        {
            Debug.LogError("Failed to spawn candy");
            return;
        }

        candyObj.transform.position = new Vector3(x * CONSTANT.TILE_SIZE, y * CONSTANT.TILE_SIZE, 0);
        candyObj.transform.SetParent(transform);

        Candy candy = candyObj.GetComponent<Candy>();
        if (candy == null)
        {
            Debug.LogError("Spawned object doesn't have Candy component");
            Destroy(candyObj);
            return;
        }

        candy.Init(x, y);
        candy.GetComponent<SpriteRenderer>().sortingOrder = candy.y;
        candies[x, y] = candy;
    }

    public void GetCandyFromWorldPosition(Vector2 worldPos, out int x, out int y)
    {
        x = Mathf.RoundToInt(worldPos.x / CONSTANT.TILE_SIZE);
        y = Mathf.RoundToInt(worldPos.y / CONSTANT.TILE_SIZE);
    }

    public void SwapCandies(Candy candy1, Candy candy2)
    {
        if (isProcessing)
            return;

        if (candy1 == null || candy2 == null)
            return;

        if (!AreCandiesAdjacent(candy1, candy2))
        {
            Debug.Log("Candies are not adjacent");
            return;
        }

        isProcessing = true;
        GameManager.Instance.CurrentState = CONSTANT.GameState.RESOLVING;

        // Hoán đổi vị trí trong mảng
        candies[candy1.x, candy1.y] = candy2;
        candies[candy2.x, candy2.y] = candy1;

        // Hoán đổi tọa độ
        (candy2.x, candy1.x) = (candy1.x, candy2.x);
        (candy2.y, candy1.y) = (candy1.y, candy2.y);

        // Animate swap
        Vector3 pos1 = new Vector3(candy1.x * CONSTANT.TILE_SIZE, candy1.y * CONSTANT.TILE_SIZE, 0);
        Vector3 pos2 = new Vector3(candy2.x * CONSTANT.TILE_SIZE, candy2.y * CONSTANT.TILE_SIZE, 0);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(candy1.transform.DOMove(pos1, 0.2f));
        sequence.Join(candy2.transform.DOMove(pos2, 0.2f));
        sequence.OnComplete(() => CheckForMatchesAfterSwap(candy1, candy2));
    }

    private bool AreCandiesAdjacent(Candy candy1, Candy candy2)
    {
        if (candy1 == null || candy2 == null)
            return false;

        int dx = Mathf.Abs(candy1.x - candy2.x);
        int dy = Mathf.Abs(candy1.y - candy2.y);

        return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
    }

    private void CheckForMatchesAfterSwap(Candy candy1, Candy candy2)
    {
        List<Candy> allMatches = new List<Candy>();

        // Kiểm tra matches từ cả 2 viên kẹo
        if (CheckForMatchAt(candy1, out List<Candy> matches1))
        {
            AddUniqueCandies(allMatches, matches1);
        }

        if (CheckForMatchAt(candy2, out List<Candy> matches2))
        {
            AddUniqueCandies(allMatches, matches2);
        }

        if (allMatches.Count > 0)
        {
            // Có match
            RemoveMatchedCandies(allMatches);

            // Chờ một chút rồi thực hiện gravity
            DOVirtual.DelayedCall(0.3f, () => ProcessGravityAndFill());
        }
        else
        {
            // Không có match, hoán đổi lại
            UndoInvalidSwap(candy1, candy2);
        }
    }

    private void RemoveMatchedCandies(List<Candy> matches)
    {
        foreach (var candy in matches)
        {
            int id = -1;
            RemoveCandy(candy);
            switch (candy.typeCandy)
            {
                case CONSTANT.TypeCandy.PURPLE:
                    id = 0;
                    break;
                case CONSTANT.TypeCandy.GREEN:
                    id = 1;
                    break;
                case CONSTANT.TypeCandy.BLUE:
                    id = 2;
                    break;
                case CONSTANT.TypeCandy.YELLOW:
                    id = 3;
                    break;
            }
            if (id != -1)
            {
                GameObject partical = Instantiate(prefabFxs[id], candy.gameObject.transform.position, candy.transform.rotation);
                Destroy(partical, 1f);
            }

        }
    }

    private void UndoInvalidSwap(Candy candy1, Candy candy2)
    {
        Debug.Log("No match found, swapping back");

        // Hoán đổi lại
        candies[candy1.x, candy1.y] = candy2;
        candies[candy2.x, candy2.y] = candy1;

        (candy2.x, candy1.x) = (candy1.x, candy2.x);
        (candy2.y, candy1.y) = (candy1.y, candy2.y);

        // Animate swap back
        Vector3 pos1 = new Vector3(candy1.x * CONSTANT.TILE_SIZE, candy1.y * CONSTANT.TILE_SIZE, 0);
        Vector3 pos2 = new Vector3(candy2.x * CONSTANT.TILE_SIZE, candy2.y * CONSTANT.TILE_SIZE, 0);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(candy1.transform.DOMove(pos1, 0.2f));
        sequence.Join(candy2.transform.DOMove(pos2, 0.2f));
        sequence.OnComplete(() =>
        {
            isProcessing = false;
            GameManager.Instance.CurrentState = CONSTANT.GameState.IDLE;
        });
    }

    private void RemoveCandyAt(int x, int y)
    {
        if (!IsValidPosition(x, y))
            return;

        Candy candy = candies[x, y];
        if (candy != null)
        {
            Destroy(candy.gameObject);
            candies[x, y] = null;
        }
    }

    public void RemoveCandy(Candy candy)
    {
        if (candy == null)
            return;

        int x = candy.x;
        int y = candy.y;

        if (IsValidPosition(x, y) && candies[x, y] == candy)
        {
            RemoveCandyAt(x, y);
        }
    }

    public bool CheckForMatchAt(Candy candy, out List<Candy> matchedCandies)
    {
        matchedCandies = new List<Candy>();

        if (candy == null)
            return false;

        CheckHorizontalMatches(candy, matchedCandies);
        CheckVerticalMatches(candy, matchedCandies);

        return matchedCandies.Count >= 3;
    }

    private void CheckHorizontalMatches(Candy candy, List<Candy> matchedCandies)
    {
        if (candy == null) return;

        List<Candy> horizontalMatches = new List<Candy>();
        horizontalMatches.Add(candy);

        // Check left
        CheckDirection(candy, -1, 0, horizontalMatches);

        // Check right
        CheckDirection(candy, 1, 0, horizontalMatches);

        if (horizontalMatches.Count >= 3)
        {
            AddUniqueCandies(matchedCandies, horizontalMatches);
        }
    }

    private void CheckVerticalMatches(Candy candy, List<Candy> matchedCandies)
    {
        if (candy == null) return;

        List<Candy> verticalMatches = new List<Candy>();
        verticalMatches.Add(candy);

        // Check down
        CheckDirection(candy, 0, -1, verticalMatches);

        // Check up
        CheckDirection(candy, 0, 1, verticalMatches);

        if (verticalMatches.Count >= 3)
        {
            AddUniqueCandies(matchedCandies, verticalMatches);
        }
    }

    private void CheckDirection(Candy startCandy, int dirX, int dirY, List<Candy> matches)
    {
        Candy current = startCandy;

        while (true)
        {
            int nextX = current.x + dirX;
            int nextY = current.y + dirY;

            Candy nextCandy = GetCandyAt(nextX, nextY);

            if (nextCandy == null || nextCandy.typeCandy != startCandy.typeCandy)
                break;

            matches.Add(nextCandy);
            current = nextCandy;
        }
    }

    public List<Candy> GetAllMatches()
    {
        HashSet<Candy> allMatchedCandies = new HashSet<Candy>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Candy candy = candies[x, y];
                if (candy != null && CheckForMatchAt(candy, out List<Candy> matchedCandies))
                {
                    AddUniqueCandies(allMatchedCandies, matchedCandies);
                }
            }
        }

        return new List<Candy>(allMatchedCandies);
    }

    private void AddUniqueCandies(ICollection<Candy> target, IEnumerable<Candy> source)
    {
        foreach (Candy candy in source)
        {
            if (candy != null && !target.Contains(candy))
            {
                target.Add(candy);
            }
        }
    }

    public bool HasEmptyCell()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (candies[x, y] == null)
                    return true;
            }
        }
        return false;
    }

    private void RefillGridUntilNoMatches()
    {
        List<Candy> matchedCandies = GetAllMatches();

        while (matchedCandies.Count > 0)
        {
            matchedCandies.ForEach(x => RemoveCandy(x));
            RefillEmptyCells();
            matchedCandies = GetAllMatches();
        }
    }

    private void RefillEmptyCells()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (candies[x, y] == null)
                {
                    SpawnNewCandyAt(x, y);
                }
            }
        }
    }

    private void ProcessGravityAndFill()
    {
        ApplyGravity();
        FillTopCells();

        // Kiểm tra matches sau khi rơi
        DOVirtual.DelayedCall(0.4f, () => CheckMatchesAfterGravity());
    }

    private void ApplyGravity()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (candies[x, y] == null)
                {
                    // Tìm kẹo phía trên để rơi xuống
                    for (int k = y + 1; k < height; k++)
                    {
                        Candy candyAbove = candies[x, k];
                        if (candyAbove != null)
                        {
                            MoveCandyDown(candyAbove, x, y);
                            break;
                        }
                    }
                }
            }
        }
    }

    private void MoveCandyDown(Candy candy, int newX, int newY)
    {
        if (candy == null)
            return;

        int oldX = candy.x;
        int oldY = candy.y;

        // Cập nhật mảng
        candies[oldX, oldY] = null;
        candies[newX, newY] = candy;

        // Cập nhật vị trí candy
        candy.Init(newX, newY);
        candy.GetComponent<SpriteRenderer>().sortingOrder = candy.y;

        // Di chuyển với animation
        candy.transform.DOMove(
            new Vector3(newX * CONSTANT.TILE_SIZE, newY * CONSTANT.TILE_SIZE, 0),
            0.2f
        );
    }

    private void FillTopCells()
    {
        // Fill tất cả các ô trống
        for (int x = 0; x < width; x++)
        {
            int emptyCount = 0;

            // Đếm và fill từ dưới lên trên
            for (int y = 0; y < height; y++)
            {
                if (candies[x, y] == null)
                {
                    emptyCount++;
                }
            }

            // Fill candy mới từ trên xuống
            for (int y = height - 1; y >= 0; y--)
            {
                if (candies[x, y] == null)
                {
                    // Tạo candy mới
                    int randomIndex = Random.Range(0, prefabCandies.Length);
                    GameObject candyObj = Instantiate(prefabCandies[randomIndex]);

                    candyObj.transform.SetParent(transform);

                    // Spawn từ trên cùng với offset
                    float spawnHeight = height + (height - y);
                    candyObj.transform.position = new Vector3(x * CONSTANT.TILE_SIZE, spawnHeight * CONSTANT.TILE_SIZE, 0);

                    Candy candy = candyObj.GetComponent<Candy>();
                    candy.Init(x, y);
                    candy.GetComponent<SpriteRenderer>().sortingOrder = candy.y;
                    candies[x, y] = candy;

                    candy.transform.DOMove(
                        new Vector3(x * CONSTANT.TILE_SIZE, y * CONSTANT.TILE_SIZE, 0),
                        0.2f
                    );
                }
            }
        }
    }
    private void CheckMatchesAfterGravity()
    {
        List<Candy> newMatches = GetAllMatches();

        if (newMatches.Count > 0)
        {
            RemoveMatchedCandies(newMatches);
            DOVirtual.DelayedCall(0.3f, () => ProcessGravityAndFill());
        }
        else
        {
            // Kết thúc
            isProcessing = false;
            GameManager.Instance.CurrentState = CONSTANT.GameState.IDLE;
        }
    }
}

// using System.Collections.Generic;
// using System.Linq;
// using DG.Tweening;
// using UnityEngine;

// class GridManager : MonoBehaviour
// {
//     public Candy[,] candies;
//     public GameObject[] prefabCandy;
//     private int width;
//     private int height;

//     public void Init()
//     {
//         width = CONSTANT.WIDTH;
//         height = CONSTANT.HEIGHT;
//         candies = new Candy[width, height];

//         for (int x = 0; x < width; x++)
//         {
//             for (int y = 0; y < height; y++)
//             {
//                 GameObject candyObj = Instantiate(prefabCandy[Random.Range(0, prefabCandy.Length)]);
//                 Candy candy = candyObj.GetComponent<Candy>();
//                 //
//                 candies[x, y] = candy;
//                 candy.Init(x, y);

//                 candyObj.transform.position = new Vector3(x * CONSTANT.TILE_SIZE, y * CONSTANT.TILE_SIZE, 0);
//                 candyObj.transform.SetParent(this.transform);

//                 Camera.main.transform.position = new Vector3((width - 1) * CONSTANT.TILE_SIZE / 2f, (height - 1) * CONSTANT.TILE_SIZE / 2f, -10);
//             }
//         }
//     }

//     public Candy GetCandyAt(Vector3 worldPos, out int x, out int y)
//     {
//         x = Mathf.RoundToInt(worldPos.x / CONSTANT.TILE_SIZE);
//         y = Mathf.RoundToInt(worldPos.y / CONSTANT.TILE_SIZE);

//         return candies[x, y];
//     }

//     public Candy GetCandy(int x, int y)
//     {
//         return candies[x, y];
//     }

//     public void SwapCandies(Candy candy1, Candy candy2)
//     {
//         if (candy1 == null || candy2 == null)
//             return;

//         // Hoán đổi vị trí trong mảng
//         candies[candy1.x, candy1.y] = candy2;
//         candies[candy2.x, candy2.y] = candy1;

//         // Hoán đổi tọa độ
//         (candy2.x, candy1.x) = (candy1.x, candy2.x);
//         (candy2.y, candy1.y) = (candy1.y, candy2.y);

//         // Hoan doi vi tri
//         Vector3 pos1 = new Vector3(candy1.x * CONSTANT.TILE_SIZE, candy1.y * CONSTANT.TILE_SIZE, 0);
//         Vector3 pos2 = new Vector3(candy2.x * CONSTANT.TILE_SIZE, candy2.y * CONSTANT.TILE_SIZE, 0);

//         candy1.transform.position = pos1;
//         candy2.transform.position = pos2;
//     }


// }