
using UnityEngine;
using DG.Tweening;
public class Candy : MonoBehaviour
{
    public Constants.CandyType typeCandy;
    public Constants.BonusType bonusType;
    public int x;
    public int y;

    public Candy Left => x > 0 ? GameManager.Instance.gridManager.candies[x - 1, y] : null;
    public Candy Right => x < Constants.WIDTH - 1 ? GameManager.Instance.gridManager.candies[x + 1, y] : null;
    public Candy Up => y < Constants.HEIGHT - 1 ? GameManager.Instance.gridManager.candies[x, y + 1] : null;
    public Candy Down => y > 0 ? GameManager.Instance.gridManager.candies[x, y - 1] : null;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Hàm này dùng khi khởi tạo hoặc khi tái sử dụng từ pool
    public void Init(int x, int y, Constants.CandyType type, Constants.BonusType bonus = Constants.BonusType.NONE, Sprite sprite = null)
    {
        this.x = x;
        this.y = y;
        this.typeCandy = type;
        this.bonusType = bonus;

        // Nếu có truyền sprite mới vào thì thay luôn
        if (sprite != null)
        {
            spriteRenderer.sprite = sprite;
        }
    }

    // HÀM MỚI: Gọi hàm này khi kẹo được nâng cấp thành Bonus
    public void UpgradeToBonus(Constants.BonusType newBonusType, Sprite newSprite)
    {
        this.bonusType = newBonusType;
        this.spriteRenderer.sprite = newSprite;

        // Có thể thêm hiệu ứng biến hình ở đây (ví dụ: scale to lên một chút rồi nhỏ lại)
        transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 10, 1);
    }
    public void Init(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public void Init(int x, int y, Constants.CandyType type)
    {
        this.x = x;
        this.y = y;
        this.typeCandy = type;
    }
}
