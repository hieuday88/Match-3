
using UnityEngine;

public class Candy : MonoBehaviour
{
    public CONSTANT.TypeCandy typeCandy;
    public int x;
    public int y;

    public Candy Left => x > 0 ? GameManager.Instance.gridManager.candies[x - 1, y] : null;
    public Candy Right => x < CONSTANT.WIDTH - 1 ? GameManager.Instance.gridManager.candies[x + 1, y] : null;
    public Candy Up => y < CONSTANT.HEIGHT - 1 ? GameManager.Instance.gridManager.candies[x, y + 1] : null;
    public Candy Down => y > 0 ? GameManager.Instance.gridManager.candies[x, y - 1] : null;
    public void Init(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}
