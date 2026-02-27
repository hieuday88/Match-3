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
        public string name;
        public Constants.CandyType colorType;
        public GameObject prefab;
        public GameObject fxPrefab;      // Nổ tròn tại chỗ

        [Header("Sweep VFX (Trail)")]
        public GameObject sweepPrefab;   // Dùng chung cho cả Ngang và Dọc

        [Header("Sprites")]
        public Sprite normalSprite;
        public Sprite stripedHorizontalSprite;
        public Sprite stripedVerticalSprite;
        public Sprite wrappedSprite;
    }
    [Header("Assets Configuration")]
    // Mảng chứa bộ ảnh cho tất cả các màu
    [SerializeField] private CandyAssetSet[] candyAssets;

    // Dictionary để tra cứu nhanh (sẽ khởi tạo trong Awake)
    private Dictionary<Constants.CandyType, CandyAssetSet> assetLookup;

    // Prefab kẹo Cầu Vồng (Vì nó không thuộc màu nào nên để riêng)
    [SerializeField] private GameObject rainbowCandyPrefab;
    // --- THÊM DÒNG NÀY ---
    [Header("Special VFX")]
    [SerializeField] private GameObject lightningPrefab;
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

        if (candies[x, y] != null)
        {
            PoolingManager.Instance.Despawn(candies[x, y].gameObject);
            candies[x, y] = null;
        }

        // Gọi hàm thuật toán lấy Màu An Toàn
        Constants.CandyType safeType = GetSafeRandomCandyType(x, y);

        // --- SỬ DỤNG DICTIONARY ĐỂ TÌM PREFAB ---
        GameObject prefabToSpawn = assetLookup[safeType].prefab;

        GameObject candyObj = PoolingManager.Instance.Spawn(prefabToSpawn);
        candyObj.transform.SetParent(transform);

        Candy candy = candyObj.GetComponent<Candy>();
        candy.Init(x, y, safeType); // Đã truyền đúng safeType
        candy.GetComponent<SpriteRenderer>().sortingOrder = y;

        Vector3 targetPos = GetWorldPosition(x, y);

        if (isInitialSpawn) candy.transform.position = targetPos;
        else candy.transform.position = targetPos + Vector3.up * Constants.TILE_SIZE;

        candies[x, y] = candy;
    }

    // --- THUẬT TOÁN NE TRÁNH MATCH-3 ---
    private Constants.CandyType GetSafeRandomCandyType(int x, int y)
    {
        // Khởi tạo danh sách các màu đang có trong candyAssets
        List<Constants.CandyType> availableTypes = new List<Constants.CandyType>();
        foreach (var asset in candyAssets)
        {
            availableTypes.Add(asset.colorType);
        }

        // Kiểm tra 2 ô bên trái
        if (x >= 2)
        {
            Candy left1 = GetCandyAt(x - 1, y);
            Candy left2 = GetCandyAt(x - 2, y);
            if (left1 != null && left2 != null && left1.typeCandy == left2.typeCandy)
            {
                availableTypes.Remove(left1.typeCandy);
            }
        }

        // Kiểm tra 2 ô bên dưới
        if (y >= 2)
        {
            Candy down1 = GetCandyAt(x, y - 1);
            Candy down2 = GetCandyAt(x, y - 2);
            if (down1 != null && down2 != null && down1.typeCandy == down2.typeCandy)
            {
                availableTypes.Remove(down1.typeCandy);
            }
        }

        // Trả về ngẫu nhiên 1 màu an toàn
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

        // --- BẮT ĐẦU KIỂM TRA CÁC TRƯỜNG HỢP ƯU TIÊN (ĐẶC QUYỀN) ---

        // 1. Kiểm tra xem có Kẹo Đặc Biệt / Cầu Vồng không?
        bool isC1Special = candy1.bonusType != Constants.BonusType.NONE;
        bool isC2Special = candy2.bonusType != Constants.BonusType.NONE;
        bool isRainbowSwap = candy1.bonusType == Constants.BonusType.RAINBOW || candy2.bonusType == Constants.BonusType.RAINBOW;

        // Nếu cả 2 đều là đặc biệt HOẶC có 1 viên là Cầu Vồng -> Chạy Siêu Combo
        if ((isC1Special && isC2Special) || isRainbowSwap)
        {
            yield return StartCoroutine(HandleSpecialComboRoutine(candy1, candy2));
        }
        else
        {
            // --- KHÔNG PHẢI KẸO ĐẶC BIỆT: XỬ LÝ MATCH 3 BÌNH THƯỜNG ---
            List<Candy> matches1 = new List<Candy>();
            List<Candy> matches2 = new List<Candy>();
            CheckForMatchAt(candy1, out matches1, out Constants.BonusType bonus1);
            CheckForMatchAt(candy2, out matches2, out Constants.BonusType bonus2);

            if (matches1.Count >= 3 || matches2.Count >= 3)
            {
                // Có Match -> Nổ
                yield return StartCoroutine(ProcessMatchesAndGravityRoutine(candy1, candy2));
            }
            else
            {
                // Thất bại -> Trượt về chỗ cũ
                SwapCandyData(candy1, candy2);

                Sequence seqBack = DOTween.Sequence();
                seqBack.Join(candy1.transform.DOMove(GetWorldPosition(candy1.x, candy1.y), Constants.SWAP_DURATION));
                seqBack.Join(candy2.transform.DOMove(GetWorldPosition(candy2.x, candy2.y), Constants.SWAP_DURATION));

                yield return seqBack.WaitForCompletion();

                isProcessing = false;
                GameManager.Instance.CurrentState = Constants.GameState.IDLE;
            }
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

    // Truyền 2 viên kẹo vừa vuốt vào đây
    private List<MatchInfo> GetAllMatchesInfo(Candy priorityCandy1 = null, Candy priorityCandy2 = null)
    {
        List<MatchInfo> allMatches = new List<MatchInfo>();
        HashSet<Candy> processedCandies = new HashSet<Candy>();

        // Hàm check nội bộ cho gọn code
        void CheckAndAdd(Candy c)
        {
            if (c != null && !processedCandies.Contains(c))
            {
                if (CheckForMatchAt(c, out List<Candy> matchedCandies, out Constants.BonusType bonus))
                {
                    MatchInfo info = new MatchInfo
                    {
                        Candies = matchedCandies,
                        Bonus = bonus,
                        CenterCandy = c
                    };
                    allMatches.Add(info);

                    // Đánh dấu các viên này đã bị nổ
                    foreach (var mc in matchedCandies) processedCandies.Add(mc);
                }
            }
        }

        // BƯỚC 1: ƯU TIÊN KIỂM TRA 2 VIÊN KẸO NGƯỜI CHƠI VỪA VUỐT
        // Điều này đảm bảo góc vuông của chữ L/T không bao giờ bị cắt xén
        CheckAndAdd(priorityCandy1);
        CheckAndAdd(priorityCandy2);

        // BƯỚC 2: QUÉT CÁC Ô CÒN LẠI (Do rơi tự do)
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CheckAndAdd(candies[x, y]);
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
    // Thêm tham số đầu vào
    private IEnumerator ProcessMatchesAndGravityRoutine(Candy swappedCandy1 = null, Candy swappedCandy2 = null)
    {
        bool hasMatches = true;

        while (hasMatches)
        {
            List<MatchInfo> matchInfos = GetAllMatchesInfo(swappedCandy1, swappedCandy2);
            swappedCandy1 = null;
            swappedCandy2 = null;

            if (matchInfos.Count > 0)
            {
                List<Candy> initialDestruction = new List<Candy>();
                bool hasMergeAnim = false; // Biến kiểm tra xem có hoạt ảnh hút kẹo không

                foreach (MatchInfo info in matchInfos)
                {
                    // ==========================================================
                    // BẢN VÁ LỖI TÂM ĐIỂM: CĂN GIỮA CHO KẸO RƠI TỰ DO
                    // ==========================================================
                    // Nếu viên kẹo này KHÔNG PHẢI do người chơi chủ động vuốt
                    if (info.CenterCandy != swappedCandy1 && info.CenterCandy != swappedCandy2)
                    {
                        // Chỉ căn giữa cho kẹo thẳng (Sọc, Cầu vồng). 
                        // Riêng Kẹo Gói (BOMB) thì tâm của nó mặc định đã nằm chuẩn ở góc vuông rồi.
                        if (info.Bonus == Constants.BonusType.ROW_CLEAR ||
                            info.Bonus == Constants.BonusType.COLUMN_CLEAR ||
                            info.Bonus == Constants.BonusType.RAINBOW)
                        {
                            // Mẹo toán học: Sắp xếp theo (x + y) sẽ tự xếp thẳng hàng cho cả trục dọc lẫn ngang
                            var sortedCandies = info.Candies.OrderBy(c => c.x + c.y).ToList();

                            // Bốc viên kẹo nằm ở chính giữa danh sách làm tâm mới
                            info.CenterCandy = sortedCandies[sortedCandies.Count / 2];
                        }
                    }
                    // ==========================================================

                    // --- NẾU LÀ TẠO KẸO ĐẶC BIỆT -> CHẠY HOẠT ẢNH HÚT KẸO ---
                    if (info.Bonus != Constants.BonusType.NONE)
                    {
                        hasMergeAnim = true;
                        Vector3 centerPos = info.CenterCandy.transform.position;

                        foreach (Candy c in info.Candies)
                        {
                            if (c != info.CenterCandy)
                            {
                                // --- BẢN VÁ LỖI: KIỂM TRA KẸO ĐẶC BIỆT ---
                                if (c.bonusType != Constants.BonusType.NONE)
                                {
                                    // Kẹo này là kẹo đặc biệt từ trước! KHÔNG HÚT NÓ!
                                    // Ném nó vào danh sách nổ để lát nữa Động cơ nổ dây chuyền kích hoạt nó
                                    initialDestruction.Add(c);
                                }
                                else
                                {
                                    // Là kẹo thường -> Hút vào tâm và biến mất
                                    // 1. Gỡ kẹo ra khỏi Data ngay lập tức
                                    candies[c.x, c.y] = null;

                                    // 2. Animation: Bay hút vào tâm và teo nhỏ lại
                                    c.transform.DOMove(centerPos, 0.25f).SetEase(Ease.InBack);
                                    c.transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack).OnComplete(() =>
                                    {
                                        c.transform.DOKill();
                                        Destroy(c.gameObject);
                                    });

                                    // 3. Xóa xác GameObject sau khi bay xong
                                    Destroy(c.gameObject, 0.3f);
                                }
                            }
                        }

                        // Nâng cấp viên trung tâm (Dùng Delay để đợi bọn kia bay vào xong mới biến hình)
                        Constants.BonusType currentBonus = info.Bonus;
                        Candy centerCandy = info.CenterCandy;

                        DOVirtual.DelayedCall(0.25f, () =>
                        {
                            if (centerCandy == null) return; // Tránh lỗi null rác

                            if (currentBonus == Constants.BonusType.RAINBOW)
                            {
                                SpawnRainbowCandyAt(centerCandy.x, centerCandy.y);
                            }
                            else
                            {
                                Sprite newSprite = GetBonusSprite(centerCandy.typeCandy, currentBonus);
                                if (newSprite != null)
                                {
                                    centerCandy.UpgradeToBonus(currentBonus, newSprite);
                                }
                            }
                        });

                        // Gỡ các viên kẹo này ra khỏi danh sách bị nổ bình thường
                        info.Candies.Clear();
                    }
                    else
                    {
                        // LÀ MATCH-3 BÌNH THƯỜNG -> Gom vào danh sách chờ nổ
                        initialDestruction.AddRange(info.Candies);
                    }
                }

                // --- XỬ LÝ NỔ KẸO THƯỜNG & NỔ DÂY CHUYỀN ---
                List<Candy> finalDestructionList = GetAffectedCandies(initialDestruction);

                // GỌI COROUTINE CHỜ XÓA KẸO
                yield return StartCoroutine(RemoveMatchedCandiesRoutine(finalDestructionList));

                // --- CHỜ HOẠT ẢNH ---
                if (hasMergeAnim)
                {
                    yield return new WaitForSeconds(0.2f);
                }
                else
                {
                    yield return new WaitForSeconds(0.1f);
                }

                // --- TRỌNG LỰC VÀ BÙ KẸO ---
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

    // --- 3. TRỌNG LỰC (VẬT LÝ RƠI TỰ DO) ---
    private IEnumerator ApplyGravityAndRefillRoutine()
    {
        bool isAnimating = false;
        float maxCalculatedTime = 0f;

        // Gia tốc trọng trường (Càng to rơi càng nhanh, bạn có thể tinh chỉnh 30-50 tùy ý)
        float gravity = 100f;
        float bounceTime = 0.04f;   // Thời gian nảy

        for (int x = 0; x < width; x++)
        {
            int emptySpaces = 0;

            // --- BƯỚC A: KÉO KẸO CŨ XUỐNG ---
            for (int y = 0; y < height; y++)
            {
                if (candies[x, y] == null)
                {
                    emptySpaces++;
                }
                else if (emptySpaces > 0)
                {
                    Candy candy = candies[x, y];
                    int newY = y - emptySpaces;

                    candies[x, newY] = candy;
                    candies[x, y] = null;

                    candy.Init(x, newY, candy.typeCandy);
                    candy.GetComponent<SpriteRenderer>().sortingOrder = newY;

                    // Tính khoảng cách và áp dụng công thức t = sqrt(2S/g)
                    float distance = y - newY;
                    float fallTime = Mathf.Sqrt((2f * distance) / gravity);
                    if (fallTime > maxCalculatedTime) maxCalculatedTime = fallTime;

                    Vector3 targetPos = GetWorldPosition(x, newY);

                    // Xây dựng chuỗi hoạt ảnh: Rơi (InQuad) -> Đập đất nảy lên (OutQuad) -> Rớt xuống lại (InQuad)
                    candy.transform.DOMove(targetPos, fallTime).SetEase(Ease.InQuad);
                    isAnimating = true;
                }
            }

            // --- BƯỚC B: BÙ KẸO MỚI TỪ TRÊN TRỜI RƠI XUỐNG ---
            for (int i = 0; i < emptySpaces; i++)
            {
                int targetY = height - emptySpaces + i;

                Constants.CandyType randomType = candyAssets[Random.Range(0, candyAssets.Length)].colorType;
                GameObject prefabToSpawn = assetLookup[randomType].prefab;

                GameObject candyObj = PoolingManager.Instance.Spawn(prefabToSpawn);
                candyObj.transform.SetParent(transform);

                Candy candy = candyObj.GetComponent<Candy>();
                candy.Init(x, targetY, randomType);
                candy.GetComponent<SpriteRenderer>().sortingOrder = targetY;

                candies[x, targetY] = candy;

                // Đặt kẹo mới ở ngoài màn hình (xếp hàng cao tít mù khơi)
                float spawnY = height + i + 1;
                candy.transform.position = GetWorldPosition(x, (int)spawnY);
                Vector3 targetPos = GetWorldPosition(x, targetY);

                // Tính thời gian rơi cho kẹo mới
                float distance = spawnY - targetY;
                float fallTime = Mathf.Sqrt((2f * distance) / gravity);
                if (fallTime > maxCalculatedTime) maxCalculatedTime = fallTime;

                candy.transform.DOMove(targetPos, fallTime).SetEase(Ease.InQuad);
                isAnimating = true;
            }
        }

        // --- BƯỚC C: CHỜ TẤT CẢ RƠI & NẢY XONG ---
        if (isAnimating)
        {
            // Chờ thời gian rơi của viên rơi lâu nhất + tổng thời gian của 2 nhịp nảy
            yield return new WaitForSeconds(maxCalculatedTime + (bounceTime * 2));
        }
    }

    private Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(x * Constants.TILE_SIZE, y * Constants.TILE_SIZE, 0);
    }

    private IEnumerator RemoveMatchedCandiesRoutine(List<Candy> matches)
    {
        bool hasBomb = false;

        // 1. Phóng to Kẹo Gói (Bomb) trước
        foreach (var candy in matches)
        {
            if (candy != null && candy.bonusType == Constants.BonusType.BOMB)
            {
                hasBomb = true;
                candy.GetComponent<SpriteRenderer>().sortingOrder = 200;
                candy.transform.DOScale(Vector3.one * 5f, 0.3f).SetEase(Ease.InBack);
            }
        }

        // 2. Chờ 0.3s cho Bomb phình to xong
        if (hasBomb)
        {
            yield return new WaitForSeconds(0.3f);
        }

        // 3. Kích nổ đồng loạt toàn bộ danh sách
        foreach (var candy in matches)
        {
            if (candy == null) continue;

            if (assetLookup.TryGetValue(candy.typeCandy, out CandyAssetSet set))
            {
                if (set.fxPrefab != null)
                {
                    GameObject particle = Instantiate(set.fxPrefab, candy.transform.position, candy.transform.rotation);
                    if (candy.bonusType == Constants.BonusType.BOMB)
                    {
                        particle.transform.localScale = Vector3.one * 2f;
                        particle.GetComponent<Renderer>().sortingOrder = 210;
                    }
                    Destroy(particle, 1.5f);
                }

                if (candy.bonusType == Constants.BonusType.ROW_CLEAR) SweepRow(candy.x, candy.y, set.sweepPrefab);
                else if (candy.bonusType == Constants.BonusType.COLUMN_CLEAR) SweepColumn(candy.x, candy.y, set.sweepPrefab);
            }

            RemoveCandy(candy);
        }
    }
    private void RemoveCandyAt(int x, int y)
    {
        if (!IsValidPosition(x, y))
            return;

        Candy candy = candies[x, y];
        if (candy != null)
        {
            candy.transform.DOKill();
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
            Debug.Log("Match hình chữ L hoặc T - Sinh Kẹo Gói");
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

    // Tính toán tất cả các viên kẹo sẽ bị phá hủy (Bao gồm nổ dây chuyền)
    private List<Candy> GetAffectedCandies(List<Candy> initialCandies)
    {
        // Dùng Queue (hàng đợi) để xử lý nổ lan
        Queue<Candy> checkQueue = new Queue<Candy>(initialCandies);
        HashSet<Candy> toDestroy = new HashSet<Candy>(initialCandies);

        while (checkQueue.Count > 0)
        {
            Candy current = checkQueue.Dequeue();
            List<Candy> bonusCandies = new List<Candy>();

            // 1. NỔ SỌC NGANG (Phá cả hàng)
            if (current.bonusType == Constants.BonusType.ROW_CLEAR)
            {
                for (int x = 0; x < width; x++)
                    bonusCandies.Add(candies[x, current.y]);
            }
            // 2. NỔ SỌC DỌC (Phá cả cột)
            else if (current.bonusType == Constants.BonusType.COLUMN_CLEAR)
            {
                for (int y = 0; y < height; y++)
                    bonusCandies.Add(candies[current.x, y]);
            }
            // 3. NỔ GÓI (Phá 3x3 xung quanh)
            else if (current.bonusType == Constants.BonusType.BOMB)
            {
                for (int x = current.x - 1; x <= current.x + 1; x++)
                {
                    for (int y = current.y - 1; y <= current.y + 1; y++)
                    {
                        if (IsValidPosition(x, y)) bonusCandies.Add(candies[x, y]);
                    }
                }
            }
            // 4. KẸO CẦU VỒNG (Vô tình bị nổ trúng -> Phá ngẫu nhiên 1 màu)
            else if (current.bonusType == Constants.BonusType.RAINBOW)
            {
                Constants.CandyType randomColor = (Constants.CandyType)Random.Range(0, 4);
                for (int x = 0; x < width; x++)
                    for (int y = 0; y < height; y++)
                        if (candies[x, y] != null && candies[x, y].typeCandy == randomColor)
                            bonusCandies.Add(candies[x, y]);
            }

            // Quét danh sách kẹo mới bị cuốn vào vụ nổ
            foreach (Candy c in bonusCandies)
            {
                if (c != null && !toDestroy.Contains(c))
                {
                    toDestroy.Add(c);
                    // Nếu nạn nhân cũng là Kẹo Đặc Biệt -> Cho vào hàng đợi nổ tiếp! (Chain Reaction)
                    if (c.bonusType != Constants.BonusType.NONE)
                    {
                        checkQueue.Enqueue(c);
                    }
                }
            }
        }

        return toDestroy.ToList();
    }

    private IEnumerator HandleSpecialComboRoutine(Candy c1, Candy c2)
    {
        Debug.Log("🔥 KÍCH HOẠT SIÊU COMBO! 🔥");
        List<Candy> triggerList = new List<Candy>();

        bool isC1Rainbow = c1.bonusType == Constants.BonusType.RAINBOW;
        bool isC2Rainbow = c2.bonusType == Constants.BonusType.RAINBOW;

        // Tâm điểm vụ nổ thường sẽ nằm ở viên kẹo thứ 2 (viên mà người chơi kéo tới)
        int centerX = c2.x;
        int centerY = c2.y;

        // ==========================================
        // 1. CẦU VỒNG + CẦU VỒNG (Nổ sạch bàn cờ)
        // ==========================================
        if (isC1Rainbow && isC2Rainbow)
        {
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (candies[x, y] != null) triggerList.Add(candies[x, y]);
        }
        // ==========================================
        // 2. CẦU VỒNG + KẸO THƯỜNG / KẸO ĐẶC BIỆT
        // ==========================================
        else if (isC1Rainbow || isC2Rainbow)
        {
            Candy rainbow = isC1Rainbow ? c1 : c2;
            Candy target = isC1Rainbow ? c2 : c1;

            Constants.CandyType targetColor = target.typeCandy;
            Constants.BonusType targetBonus = target.bonusType;

            triggerList.Add(rainbow);

            // NẾU VUỐT CẦU VỒNG + KẸO THƯỜNG
            if (targetBonus == Constants.BonusType.NONE)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        if (candies[x, y] != null && candies[x, y].typeCandy == targetColor)
                        {
                            triggerList.Add(candies[x, y]);

                            // --- KÍCH HOẠT TIA SÉT ---
                            ShootLightning(rainbow.transform.position, candies[x, y].transform.position);
                        }
                    }
                }

                // CỰC KỲ QUAN TRỌNG: Dừng lại 0.4s để người chơi ngắm tia sét giật tung tóe rồi mới cho kẹo nổ!
                yield return new WaitForSeconds(0.4f);
            }
            // NẾU CẦU VỒNG + KẸO ĐẶC BIỆT (Sọc, Gói)
            else
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Candy currentCandy = candies[x, y];
                        if (currentCandy != null && currentCandy.typeCandy == targetColor)
                        {
                            Constants.BonusType assignedBonus = targetBonus;
                            if (targetBonus == Constants.BonusType.ROW_CLEAR || targetBonus == Constants.BonusType.COLUMN_CLEAR)
                            {
                                assignedBonus = Random.value > 0.5f ? Constants.BonusType.ROW_CLEAR : Constants.BonusType.COLUMN_CLEAR;
                            }

                            Sprite newSprite = GetBonusSprite(currentCandy.typeCandy, assignedBonus);
                            currentCandy.UpgradeToBonus(assignedBonus, newSprite);
                            triggerList.Add(currentCandy);

                            // --- KÍCH HOẠT TIA SÉT CHẠY ĐẾN ĐỂ "BIẾN HÌNH" ---
                            ShootLightning(rainbow.transform.position, currentCandy.transform.position);
                        }
                    }
                }
                // Dừng lại đợi kẹo biến hình xong
                yield return new WaitForSeconds(0.4f);
            }
        }
        // ==========================================
        // 3. KẸO SỌC + KẸO GÓI (Chữ thập khổng lồ 3 hàng 3 cột)
        // ==========================================
        else if ((c1.bonusType == Constants.BonusType.ROW_CLEAR || c1.bonusType == Constants.BonusType.COLUMN_CLEAR) && c2.bonusType == Constants.BonusType.BOMB ||
                 (c2.bonusType == Constants.BonusType.ROW_CLEAR || c2.bonusType == Constants.BonusType.COLUMN_CLEAR) && c1.bonusType == Constants.BonusType.BOMB)
        {
            for (int i = -1; i <= 1; i++) // Phạm vi -1, 0, +1 (Rộng 3 ô)
            {
                // Quét 3 Hàng ngang
                if (centerY + i >= 0 && centerY + i < height)
                    for (int x = 0; x < width; x++)
                        if (candies[x, centerY + i] != null) triggerList.Add(candies[x, centerY + i]);

                // Quét 3 Cột dọc
                if (centerX + i >= 0 && centerX + i < width)
                    for (int y = 0; y < height; y++)
                        if (candies[centerX + i, y] != null) triggerList.Add(candies[centerX + i, y]);
            }
        }
        // ==========================================
        // 4. KẸO GÓI + KẸO GÓI (Vùng nổ siêu rộng 5x5)
        // ==========================================
        else if (c1.bonusType == Constants.BonusType.BOMB && c2.bonusType == Constants.BonusType.BOMB)
        {
            for (int x = centerX - 2; x <= centerX + 2; x++)
            {
                for (int y = centerY - 2; y <= centerY + 2; y++)
                {
                    if (IsValidPosition(x, y) && candies[x, y] != null)
                        triggerList.Add(candies[x, y]);
                }
            }
        }
        // ==========================================
        // 5. KẸO SỌC + KẸO SỌC (Chữ thập nhỏ 1 hàng 1 cột)
        // ==========================================
        else if ((c1.bonusType == Constants.BonusType.ROW_CLEAR || c1.bonusType == Constants.BonusType.COLUMN_CLEAR) &&
                 (c2.bonusType == Constants.BonusType.ROW_CLEAR || c2.bonusType == Constants.BonusType.COLUMN_CLEAR))
        {
            // Bắt buộc 1 viên nổ ngang, 1 viên nổ dọc
            c1.bonusType = Constants.BonusType.ROW_CLEAR;
            c2.bonusType = Constants.BonusType.COLUMN_CLEAR;
            triggerList.Add(c1);
            triggerList.Add(c2);
        }

        // --- GIAI ĐOẠN CUỐI: KÍCH NỔ TOÀN BỘ ---
        List<Candy> finalDestructionList = GetAffectedCandies(triggerList);

        // GỌI COROUTINE CHỜ XÓA KẸO
        yield return StartCoroutine(RemoveMatchedCandiesRoutine(finalDestructionList));

        yield return new WaitForSeconds(0.1f); // Chờ dư âm vụ nổ

        // Rơi kẹo xuống
        yield return StartCoroutine(ApplyGravityAndRefillRoutine());

        // Vòng lặp dọn dẹp (nếu kẹo mới rơi xuống lại tự động Match-3)
        yield return StartCoroutine(ProcessMatchesAndGravityRoutine());
    }

    // --- HIỆU ỨNG QUÉT HÀNG NGANG ---
    private void SweepRow(int x, int y, GameObject sweepPrefab)
    {
        if (sweepPrefab == null) return;

        // Cho bay lố ra hẳn 15 ô về mỗi phía để chắc chắn bay ra khỏi màn hình
        int overShoot = 10;

        Vector3 centerPos = GetWorldPosition(x, y);
        Vector3 leftEndPos = GetWorldPosition(-overShoot, y);
        Vector3 rightEndPos = GetWorldPosition(width + overShoot, y);

        // Quãng đường dài hơn nên ta tăng thời gian lên một chút (hoặc bạn có thể tự chỉnh)
        float duration = 0.8f;

        GameObject effectLeft = Instantiate(sweepPrefab, centerPos + new Vector3(1, 0, 0), Quaternion.identity);
        effectLeft.transform.DOMove(leftEndPos, duration)
            .SetEase(Ease.OutQuad) // Nổ bùng ra cực nhanh lúc đầu, mượt về sau
            .OnComplete(() => Destroy(effectLeft));

        GameObject effectRight = Instantiate(sweepPrefab, centerPos + new Vector3(-1, 0, 0), Quaternion.identity);
        effectRight.transform.DOMove(rightEndPos, duration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => Destroy(effectRight));
    }

    // --- HIỆU ỨNG QUÉT HÀNG DỌC ---
    private void SweepColumn(int x, int y, GameObject sweepPrefab)
    {
        if (sweepPrefab == null) return;

        int overShoot = 10;

        Vector3 centerPos = GetWorldPosition(x, y);
        Vector3 bottomEndPos = GetWorldPosition(x, -overShoot);
        Vector3 topEndPos = GetWorldPosition(x, height + overShoot);

        float duration = 0.8f;

        GameObject effectBottom = Instantiate(sweepPrefab, centerPos + new Vector3(0, 1, 0), Quaternion.identity);
        effectBottom.transform.DOMove(bottomEndPos, duration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => Destroy(effectBottom));

        GameObject effectTop = Instantiate(sweepPrefab, centerPos + new Vector3(0, -1, 0), Quaternion.identity);
        effectTop.transform.DOMove(topEndPos, duration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => Destroy(effectTop));
    }

    // --- HÀM VẼ TIA SÉT ---
    private void ShootLightning(Vector3 startPos, Vector3 endPos)
    {
        if (lightningPrefab == null) return;

        // Sinh ra tia sét
        GameObject lightningObj = Instantiate(lightningPrefab, Vector3.zero, Quaternion.identity);
        LineRenderer lr = lightningObj.GetComponent<LineRenderer>();

        // Cắm 2 đầu tia sét vào 2 viên kẹo
        lr.SetPosition(0, startPos);
        lr.SetPosition(1, endPos);

        // Hiệu ứng Fade out (Mờ dần) rồi biến mất
        Color startColor = Color.white;
        Color endColor = new Color(1, 1, 1, 0); // Mờ trong suốt

        // Dùng DOTween làm mờ tia sét trong 0.4s
        lr.DOColor(new Color2(startColor, startColor), new Color2(endColor, endColor), 0.4f)
            .SetEase(Ease.InQuad)
            .OnComplete(() => Destroy(lightningObj));
    }

}
