using System.Collections.Generic;
using System.Collections;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    // --- THÊM MỚI: Struct để quản lý bộ ảnh cho từng màu ---
    [System.Serializable]
    public struct CandyAssetSet
    {
        public string name; // Đặt tên cho dễ nhìn trong Inspector (VD: "Set Đỏ")
        public Constants.CandyType colorType;
        public Sprite normalSprite;
        public Sprite stripedHorizontalSprite; // Sọc ngang
        public Sprite stripedVerticalSprite;   // Sọc dọc
        public Sprite wrappedSprite;           // Kẹo gói (Bomb)
    }

    [Header("Assets Configuration")]
    // Mảng chứa bộ ảnh cho tất cả các màu
    [SerializeField] private CandyAssetSet[] candyAssets;

    // Dictionary để tra cứu nhanh (sẽ khởi tạo trong Awake)
    private Dictionary<Constants.CandyType, CandyAssetSet> assetLookup;

    // Prefab kẹo Cầu Vồng (Vì nó không thuộc màu nào nên để riêng)
    [SerializeField] private GameObject rainbowCandyPrefab;
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
        width = Constants.WIDTH;
        height = Constants.HEIGHT;
        candies = new Candy[width, height];

        assetLookup = new Dictionary<Constants.CandyType, CandyAssetSet>();
        foreach (var set in candyAssets)
        {
            if (!assetLookup.ContainsKey(set.colorType))
            {
                assetLookup.Add(set.colorType, set);
            }
        }

        PositionCamera();
        InitializeGrid();
    }

    private void PositionCamera()
        => Camera.main.transform.position = new Vector3((width - 1) * Constants.TILE_SIZE / 2f, (height - 1) * Constants.TILE_SIZE / 2f, -10);

    private void InitializeGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                SpawnNewCandyAt(x, y, true);
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

        candyObj.transform.position = new Vector3(x * Constants.TILE_SIZE, y * Constants.TILE_SIZE, 0);
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

    private void SpawnRainbowCandyAt(int x, int y)
    {
        if (!IsValidPosition(x, y))
            return;

        RemoveCandyAt(x, y);

        GameObject candyObj = Instantiate(rainbowCandyPrefab);

        if (candyObj == null)
        {
            Debug.LogError("Failed to spawn rainbow candy");
            return;
        }

        candyObj.transform.position = new Vector3(x * Constants.TILE_SIZE, y * Constants.TILE_SIZE, 0);
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

    // --- REFACTOR: SPAWN THÔNG MINH ---

    private void SpawnNewCandyAt(int x, int y, bool isInitialSpawn = false)
    {
        if (!IsValidPosition(x, y)) return;

        // 1. Trả kẹo cũ về Pool thay vì Destroy (nếu có)
        if (candies[x, y] != null)
        {
            PoolingManager.Instance.Despawn(candies[x, y].gameObject);
            candies[x, y] = null;
        }

        // 2. Thuật toán Smart Spawning: Tìm màu an toàn không bị match-3
        int safeTypeIndex = GetSafeRandomCandyType(x, y);

        // 3. Dùng Object Pooling thay vì Instantiate
        GameObject candyObj = PoolingManager.Instance.Spawn(prefabCandies[safeTypeIndex]);
        candyObj.transform.SetParent(transform);

        Candy candy = candyObj.GetComponent<Candy>();

        // Cần truyền Type vào hàm Init (Bạn nhớ cập nhật hàm Init trong class Candy nhé)
        candy.Init(x, y, (Constants.CandyType)safeTypeIndex);
        candy.GetComponent<SpriteRenderer>().sortingOrder = y;

        Vector3 targetPos = new Vector3(x * Constants.TILE_SIZE, y * Constants.TILE_SIZE, 0);

        if (isInitialSpawn)
        {
            // Bàn cờ lúc đầu hiện ra ngay lập tức
            candy.transform.position = targetPos;
        }
        else
        {
            // Nếu là sinh kẹo bù (Refill) lúc đang chơi, sẽ cho sinh ở trên cao ngoài màn hình
            // (Hoạt ảnh rơi xuống sẽ do hàm Gravity đảm nhiệm ở Bước 5)
            candy.transform.position = targetPos + Vector3.up * Constants.TILE_SIZE;
        }

        candies[x, y] = candy;
    }

    // --- THUẬT TOÁN NE TRÁNH MATCH-3 ---
    private int GetSafeRandomCandyType(int x, int y)
    {
        // Tạo danh sách các ID màu có thể dùng (VD: 0, 1, 2, 3)
        List<int> availableTypes = new List<int>();
        for (int i = 0; i < prefabCandies.Length; i++)
        {
            availableTypes.Add(i);
        }

        // Kiểm tra 2 ô bên trái
        if (x >= 2)
        {
            Candy left1 = GetCandyAt(x - 1, y);
            Candy left2 = GetCandyAt(x - 2, y);
            if (left1 != null && left2 != null && left1.typeCandy == left2.typeCandy)
            {
                availableTypes.Remove((int)left1.typeCandy); // Xóa màu bị trùng khỏi danh sách
            }
        }

        // Kiểm tra 2 ô bên dưới
        if (y >= 2)
        {
            Candy down1 = GetCandyAt(x, y - 1);
            Candy down2 = GetCandyAt(x, y - 2);
            if (down1 != null && down2 != null && down1.typeCandy == down2.typeCandy)
            {
                availableTypes.Remove((int)down1.typeCandy); // Xóa màu bị trùng khỏi danh sách
            }
        }

        // Trả về ngẫu nhiên 1 trong số các màu an toàn còn lại
        int randomIndex = Random.Range(0, availableTypes.Count);
        return availableTypes[randomIndex];
    }

    public void GetCandyPositionFromWorldPosition(Vector2 worldPos, out int x, out int y)
    {
        x = Mathf.RoundToInt(worldPos.x / Constants.TILE_SIZE);
        y = Mathf.RoundToInt(worldPos.y / Constants.TILE_SIZE);
    }

    public void SwapCandies(Candy candy1, Candy candy2)
    {
        if (isProcessing) return;
        StartCoroutine(SwapRoutine(candy1, candy2));
    }

    private IEnumerator SwapRoutine(Candy candy1, Candy candy2)
    {
        isProcessing = true;
        GameManager.Instance.CurrentState = Constants.GameState.SWAP;

        // Hoán đổi Data
        SwapCandyData(candy1, candy2);

        // Hoán đổi Hình ảnh (Animation)
        Sequence seq = DOTween.Sequence();
        seq.Join(candy1.transform.DOMove(GetWorldPosition(candy1.x, candy1.y), Constants.SWAP_DURATION));
        seq.Join(candy2.transform.DOMove(GetWorldPosition(candy2.x, candy2.y), Constants.SWAP_DURATION));

        yield return seq.WaitForCompletion(); // Chờ 2 viên kẹo trượt xong

        // Kiểm tra xem có tạo ra Match nào không
        List<Candy> matches1 = new List<Candy>();
        List<Candy> matches2 = new List<Candy>();
        CheckForMatchAt(candy1, out matches1, out Constants.BonusType bonus1);
        CheckForMatchAt(candy2, out matches2, out Constants.BonusType bonus2);

        if (matches1.Count >= 3 || matches2.Count >= 3)
        {
            // THÀNH CÔNG: Chuyển sang vòng lặp Nổ -> Rơi -> Nổ
            StartCoroutine(ProcessMatchesAndGravityRoutine());
        }
        else
        {
            // THẤT BẠI: Trượt về chỗ cũ
            SwapCandyData(candy1, candy2);

            Sequence seqBack = DOTween.Sequence();
            seqBack.Join(candy1.transform.DOMove(GetWorldPosition(candy1.x, candy1.y), Constants.SWAP_DURATION));
            seqBack.Join(candy2.transform.DOMove(GetWorldPosition(candy2.x, candy2.y), Constants.SWAP_DURATION));

            yield return seqBack.WaitForCompletion();

            // Mở khóa Input
            isProcessing = false;
            GameManager.Instance.CurrentState = Constants.GameState.IDLE;
        }
    }

    private void SwapCandyData(Candy c1, Candy c2)
    {
        // Đổi chỗ trong mảng
        candies[c1.x, c1.y] = c2;
        candies[c2.x, c2.y] = c1;

        // Đổi tọa độ nội tại
        int tempX = c1.x; int tempY = c1.y;
        c1.Init(c2.x, c2.y, c1.typeCandy);
        c2.Init(tempX, tempY, c2.typeCandy);

        // Chỉnh SortingOrder để lúc trượt không bị đè lỗi
        c1.GetComponent<SpriteRenderer>().sortingOrder = c1.y;
        c2.GetComponent<SpriteRenderer>().sortingOrder = c2.y;
    }

    // --- 2. VÒNG LẶP CỐT LÕI (NỔ -> RƠI -> FILL) ---
    // Class phụ để gom dữ liệu nổ lại cho gọn
    private class MatchInfo
    {
        public List<Candy> Candies;
        public Constants.BonusType Bonus;
        public Candy CenterCandy; // Viên kẹo tâm điểm để sinh kẹo đặc biệt
    }

    private List<MatchInfo> GetAllMatchesInfo()
    {
        List<MatchInfo> allMatches = new List<MatchInfo>();
        HashSet<Candy> processedCandies = new HashSet<Candy>(); // Tránh 1 vụ nổ bị quét 2 lần

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Candy candy = candies[x, y];
                if (candy != null && !processedCandies.Contains(candy))
                {
                    if (CheckForMatchAt(candy, out List<Candy> matchedCandies, out Constants.BonusType bonus))
                    {
                        MatchInfo info = new MatchInfo
                        {
                            Candies = matchedCandies,
                            Bonus = bonus,
                            CenterCandy = candy
                        };
                        allMatches.Add(info);

                        // Đánh dấu các viên này đã được tính toán để vòng for không quét lại
                        foreach (var c in matchedCandies) processedCandies.Add(c);
                    }
                }
            }
        }
        return allMatches;
    }

    // Hàm Helper để lấy Sprite dựa trên màu và loại bonus
    private Sprite GetBonusSprite(Constants.CandyType color, Constants.BonusType bonus)
    {
        if (!assetLookup.ContainsKey(color)) return null;

        CandyAssetSet set = assetLookup[color];
        switch (bonus)
        {
            case Constants.BonusType.ROW_CLEAR: return set.stripedHorizontalSprite;
            case Constants.BonusType.COLUMN_CLEAR: return set.stripedVerticalSprite;
            case Constants.BonusType.BOMB: return set.wrappedSprite;
            default: return set.normalSprite;
        }
    }
    private IEnumerator ProcessMatchesAndGravityRoutine()
    {
        bool hasMatches = true;

        while (hasMatches)
        {
            List<MatchInfo> matchInfos = GetAllMatchesInfo();

            if (matchInfos.Count > 0)
            {
                foreach (MatchInfo info in matchInfos)
                {
                    // XỬ LÝ KẸO ĐẶC BIỆT
                    if (info.Bonus != Constants.BonusType.NONE)
                    {
                        // Trường hợp Kẹo Cầu Vồng (Rainbow) - Xử lý riêng vì nó là prefab khác hẳn
                        if (info.Bonus == Constants.BonusType.RAINBOW)
                        {
                            // Với cầu vồng thì bắt buộc phải thay Prefab vì nó không có màu sắc cụ thể
                            SpawnRainbowCandyAt(info.CenterCandy.x, info.CenterCandy.y);
                            // Viên cũ vẫn nằm trong list info.Candies nên sẽ bị xóa bên dưới
                        }
                        else
                        {
                            // Trường hợp Kẹo Sọc / Kẹo Gói -> THAY SPRITE
                            Sprite newSprite = GetBonusSprite(info.CenterCandy.typeCandy, info.Bonus);
                            if (newSprite != null)
                            {
                                // Gọi hàm biến hình trên viên kẹo
                                info.CenterCandy.UpgradeToBonus(info.Bonus, newSprite);
                            }

                            // QUAN TRỌNG: Loại viên kẹo này khỏi danh sách xóa
                            info.Candies.Remove(info.CenterCandy);
                        }
                    }

                    // Xóa các viên còn lại
                    RemoveMatchedCandies(info.Candies);
                }

                yield return new WaitForSeconds(0.2f);
                yield return StartCoroutine(ApplyGravityAndRefillRoutine());
                yield return new WaitForSeconds(0.1f);
            }
            else
            {
                hasMatches = false;
            }
        }

        isProcessing = false;
        GameManager.Instance.CurrentState = Constants.GameState.IDLE;
    }

    // --- 3. TRỌNG LỰC (GRAVITY CHUẨN CANDY CRUSH) ---
    private IEnumerator ApplyGravityAndRefillRoutine()
    {
        bool isAnimating = false;
        float maxFallTime = 0.3f; // Cài đặt 1 thời gian rơi chung để tất cả chạm đất cùng lúc

        // Quét từng cột
        for (int x = 0; x < width; x++)
        {
            int emptySpaces = 0; // Đếm số lỗ hổng

            // --- BƯỚC A: KÉO KẸO CŨ XUỐNG ---
            for (int y = 0; y < height; y++)
            {
                if (candies[x, y] == null)
                {
                    emptySpaces++;
                }
                else if (emptySpaces > 0)
                {
                    // Dịch chuyển Data kẹo cũ xuống
                    Candy candy = candies[x, y];
                    int newY = y - emptySpaces;

                    candies[x, newY] = candy;
                    candies[x, y] = null;

                    candy.Init(x, newY, candy.typeCandy);
                    candy.GetComponent<SpriteRenderer>().sortingOrder = newY;

                    // Bắt đầu Animation rơi của kẹo cũ
                    candy.transform.DOMove(GetWorldPosition(x, newY), maxFallTime).SetEase(Ease.InQuad);
                    isAnimating = true;
                }
            }

            // --- BƯỚC B: BÙ KẸO MỚI NỐI ĐUÔI ---
            // Số lượng kẹo thiếu chính là emptySpaces. Ta đẻ kẹo mới trên trời và cho rơi xuống
            for (int i = 0; i < emptySpaces; i++)
            {
                int targetY = height - emptySpaces + i; // Vị trí cuối cùng nó cần nằm trên lưới

                // Lấy kẹo từ Pool
                int randomType = Random.Range(0, prefabCandies.Length);
                GameObject candyObj = PoolingManager.Instance.Spawn(prefabCandies[randomType]);
                candyObj.transform.SetParent(transform);

                Candy candy = candyObj.GetComponent<Candy>();
                candy.Init(x, targetY, (Constants.CandyType)randomType);
                candy.GetComponent<SpriteRenderer>().sortingOrder = targetY;

                candies[x, targetY] = candy;

                // Đặt kẹo mới ở ngoài màn hình (xếp hàng nối đuôi nhau chờ rơi)
                float spawnY = height + i;
                candy.transform.position = GetWorldPosition(x, (int)spawnY);

                // Bắt đầu Animation rơi của kẹo mới (Chạy song song với kẹo cũ vì không có lệnh yield cản lại)
                candy.transform.DOMove(GetWorldPosition(x, targetY), maxFallTime).SetEase(Ease.InQuad);
                isAnimating = true;
            }
        }

        // --- BƯỚC C: CHỜ TẤT CẢ RƠI XONG ---
        // Chỉ đặt duy nhất 1 lệnh yield ở cuối cùng để khóa vòng lặp chờ hoạt ảnh hoàn tất
        if (isAnimating)
        {
            yield return new WaitForSeconds(maxFallTime);
        }
    }

    private Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(x * Constants.TILE_SIZE, y * Constants.TILE_SIZE, 0);
    }

    private void RemoveMatchedCandies(List<Candy> matches)
    {
        foreach (var candy in matches)
        {
            int id = -1;
            RemoveCandy(candy);
            switch (candy.typeCandy)
            {
                case Constants.CandyType.PURPLE:
                    id = 0;
                    break;
                case Constants.CandyType.GREEN:
                    id = 1;
                    break;
                case Constants.CandyType.BLUE:
                    id = 2;
                    break;
                case Constants.CandyType.YELLOW:
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

    // Thêm out bonusType để biết match này sẽ đẻ ra kẹo gì
    public bool CheckForMatchAt(Candy candy, out List<Candy> matchedCandies, out Constants.BonusType bonusToSpawn)
    {
        matchedCandies = new List<Candy>();
        bonusToSpawn = Constants.BonusType.NONE;

        if (candy == null) return false;

        List<Candy> horMatches = new List<Candy>();
        List<Candy> verMatches = new List<Candy>();

        horMatches.Add(candy);
        verMatches.Add(candy);

        // Check Ngang
        CheckDirection(candy, -1, 0, horMatches); // Trái
        CheckDirection(candy, 1, 0, horMatches);  // Phải

        // Check Dọc
        CheckDirection(candy, 0, -1, verMatches); // Dưới
        CheckDirection(candy, 0, 1, verMatches);  // Trên

        bool hasHor = horMatches.Count >= 3;
        bool hasVer = verMatches.Count >= 3;

        if (!hasHor && !hasVer) return false;

        // --- BẮT ĐẦU NHẬN DIỆN HÌNH DÁNG ---

        if (horMatches.Count >= 5 || verMatches.Count >= 5)
        {
            // Nối 5 thẳng hàng -> Kẹo Cầu Vồng
            bonusToSpawn = Constants.BonusType.RAINBOW;
            AddUniqueCandies(matchedCandies, hasHor ? horMatches : verMatches);
        }
        else if (hasHor && hasVer)
        {
            // Nối có góc vuông (Chữ L, Chữ T) -> Kẹo Gói (BOMB)
            bonusToSpawn = Constants.BonusType.BOMB;
            AddUniqueCandies(matchedCandies, horMatches);
            AddUniqueCandies(matchedCandies, verMatches);
        }
        else if (horMatches.Count == 4)
        {
            // Nối 4 ngang -> Kẹo sọc dọc (Phá cột)
            bonusToSpawn = Constants.BonusType.COLUMN_CLEAR;
            AddUniqueCandies(matchedCandies, horMatches);
        }
        else if (verMatches.Count == 4)
        {
            // Nối 4 dọc -> Kẹo sọc ngang (Phá hàng)
            bonusToSpawn = Constants.BonusType.ROW_CLEAR;
            AddUniqueCandies(matchedCandies, verMatches);
        }
        else
        {
            // Nối 3 bình thường
            AddUniqueCandies(matchedCandies, hasHor ? horMatches : verMatches);
        }

        return true;
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

            if (nextY >= 9 || dirY >= 9) // Chỉ kiểm tra trong phần hiển thị
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
                if (candy != null && CheckForMatchAt(candy, out List<Candy> matchedCandies, out Constants.BonusType bonus))
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

}
