using DG.Tweening;
using UnityEngine;

public class Test : MonoBehaviour
{
    public GameObject EffectPrefab;
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            SweepRow(0);
        if (Input.GetKeyDown(KeyCode.C))
            SweepColumn(0);

    }

    public void SweepColumn(int x)
    {
        Debug.Log("test");
        float startY = -1;
        float endY = 10 * Constants.TILE_SIZE;

        Vector3 startPos = new Vector3(x * Constants.TILE_SIZE, startY, 0);
        Vector3 endPos = new Vector3(x * Constants.TILE_SIZE, endY, 0);

        GameObject effect = Instantiate(EffectPrefab);
        effect.transform.position = startPos;

        effect.transform.DOMove(endPos, 0.35f)
            .SetEase(Ease.Linear)
            .OnComplete(() => Destroy(effect));
    }

    public void SweepRow(int y)
    {
        float startX = -1;
        float endX = 10 * Constants.TILE_SIZE;

        Vector3 startPos = new Vector3(startX, y * Constants.TILE_SIZE, 0);
        Vector3 endPos = new Vector3(endX, y * Constants.TILE_SIZE, 0);

        GameObject effect = Instantiate(EffectPrefab);
        effect.transform.position = startPos;

        effect.transform.DOMove(endPos, 0.35f)
            .SetEase(Ease.Linear)
            .OnComplete(() => Destroy(effect));
    }
}
