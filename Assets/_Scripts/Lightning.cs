
using System.Collections;
using UnityEngine;
public class Lightning : MonoBehaviour
{
	[SerializeField] private int segmentCount = 8;
	private Vector2[] posList;
	private LineRenderer line;
	public float amplitude = 1.5f;
	public Vector2 pos1, pos2;
	public bool perlin = true;
	private float time = 0f;
	private const float noiseSpeed = 5f;

	private void Awake()
	{
		if (segmentCount < 2) segmentCount = 2;
		posList = new Vector2[segmentCount];
	}

	void StartLight()
	{
		line = GetComponent<LineRenderer>();
		time = 0f;
		StartCoroutine(StartLightning());

		Invoke("Kill", 0.5f);
	}

	IEnumerator StartLightning()
	{
		while (true)
		{
			line.positionCount = posList.Length;
			Vector2 axis = pos2 - pos1;
			Vector2 normal = axis.sqrMagnitude > 0.0001f
				? new Vector2(-axis.y, axis.x).normalized
				: Vector2.up;

			for (int i = 0; i < posList.Length; i++)
			{
				posList[i] = Vector2.Lerp(pos1, pos2, (float)i / posList.Length);
				line.SetPosition(i, posList[i]);
			}

			if (perlin)
			{
				for (int i = 1; i < posList.Length - 1; i++) // Skip edges
				{
					float noise = Mathf.PerlinNoise(time + i * 0.5f, time) * amplitude - amplitude * 0.5f;
					posList[i] += normal * noise;
					line.SetPosition(i, posList[i]);
				}
			}

			posList[0] = pos1;
			posList[posList.Length - 1] = pos2;
			line.SetPosition(0, pos1);
			line.SetPosition(posList.Length - 1, pos2);

			time += Time.deltaTime * noiseSpeed;
			yield return new WaitForEndOfFrame();
		}
	}

	void Kill()
	{
		StopAllCoroutines();
		Destroy(gameObject);
	}

	internal void SetLight(Vector3 position1, Vector3 position2)
	{
		pos1 = position1;
		pos2 = position2;

		StartLight();
	}
}

