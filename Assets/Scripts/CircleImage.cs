using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Circle Image", 12)]
public class CircleImage : Image
{
	public enum FillMode
	{
		FillInside,
		FillOutside,
		Edge
	};

	public int segment = 50;
	public FillMode fillMode;
	public float edgeThickness = 10f;

	private float halfWidth, halfHeight;
	private float offsetWidth, offsetHeight;
	private float segmentRadians;

	protected override void OnPopulateMesh(VertexHelper vh)
	{
		if (type != Type.Simple)
		{
			base.OnPopulateMesh(vh);
			return;
		}

		vh.Clear();

		Rect r = GetPixelAdjustedRect();
		halfWidth = r.width * 0.5f;
		halfHeight = r.height * 0.5f;

		Vector2 pivot = rectTransform.pivot;
		offsetWidth = r.width * (0.5f - pivot.x);
		offsetHeight = r.height * (0.5f - pivot.y);

		Color32 color32 = color;

		if (fillMode == FillMode.FillInside)
		{
			segmentRadians = 360f / segment * Mathf.Deg2Rad;
			FillInside(vh, color32);
		}
		else if (fillMode == FillMode.FillOutside)
		{
			int quarterSegment = Mathf.CeilToInt(segment / 4);
			segmentRadians = 360f / (quarterSegment * 4) * Mathf.Deg2Rad;

			vh.AddVert(new Vector3(halfWidth + offsetWidth, halfHeight + offsetHeight, 0f), color32, MapUV(new Vector2(1, 1)));
			vh.AddVert(new Vector3(-halfWidth + offsetWidth, halfHeight + offsetHeight, 0f), color32, MapUV(new Vector2(0, 1)));
			vh.AddVert(new Vector3(-halfWidth + offsetWidth, -halfHeight + offsetHeight, 0f), color32, MapUV(new Vector2(0, 0)));
			vh.AddVert(new Vector3(halfWidth + offsetWidth, -halfHeight + offsetHeight, 0f), color32, MapUV(new Vector2(1, 0)));

			int triIdx = 4;
			FillOutside(vh, new Vector3(halfWidth + offsetWidth, offsetHeight, 0f), 0, quarterSegment, ref triIdx, color32);
			FillOutside(vh, new Vector3(offsetWidth, halfHeight + offsetHeight, 0f), 1, quarterSegment, ref triIdx, color32);
			FillOutside(vh, new Vector3(-halfWidth + offsetWidth, offsetHeight, 0f), 2, quarterSegment, ref triIdx, color32);
			FillOutside(vh, new Vector3(offsetWidth, -halfHeight + offsetHeight, 0f), 3, quarterSegment, ref triIdx, color32);
		}
		else
		{
			segmentRadians = 360f / segment * Mathf.Deg2Rad;
			GenerateEdges(vh, color32);
		}
	}

	private void FillInside(VertexHelper vh, Color32 color32)
	{
		vh.AddVert(new Vector3(offsetWidth, offsetHeight, 0f), color32, MapUV(new Vector2(0.5f, 0.5f)));
		vh.AddVert(new Vector3(halfWidth + offsetWidth, offsetHeight, 0f), color32, MapUV(new Vector2(1, 0.5f)));

		int triIdx = 2;
		for (int i = 1; i < segment; i++, triIdx++)
		{
			float radians = i * segmentRadians;
			Vector2 uvPos = MapUV(new Vector2(Mathf.Cos(radians) * 0.5f + 0.5f, Mathf.Sin(radians) * 0.5f + 0.5f));
			vh.AddVert(new Vector3(Mathf.Cos(radians) * halfWidth + offsetWidth, Mathf.Sin(radians) * halfHeight + offsetHeight, 0f), color32, uvPos);
			vh.AddTriangle(triIdx, triIdx - 1, 0);
		}
		vh.AddTriangle(1, triIdx - 1, 0);
	}

	private void FillOutside(VertexHelper vh, Vector3 initialPoint, int quarterIndex, int quarterSegment, ref int triIdx, Color32 color32)
	{
		int startIdx = quarterIndex * quarterSegment;
		int endIdx = (quarterIndex + 1) * quarterSegment;
		vh.AddVert(initialPoint, color32, MapUV(new Vector2(0.5f + (initialPoint.x - offsetWidth) / halfWidth * 0.5f, 0.5f + (initialPoint.y - offsetHeight) / halfHeight * 0.5f)));
		triIdx++;
		for (int i = startIdx + 1; i <= endIdx; i++, triIdx++)
		{
			float radians = i * segmentRadians;
			Vector2 uvPos = MapUV(new Vector2(Mathf.Cos(radians) * 0.5f + 0.5f, Mathf.Sin(radians) * 0.5f + 0.5f));
			vh.AddVert(new Vector3(Mathf.Cos(radians) * halfWidth + offsetWidth, Mathf.Sin(radians) * halfHeight + offsetHeight, 0f), color32, uvPos);
			vh.AddTriangle(quarterIndex, triIdx - 1, triIdx);
		}
	}

	private void GenerateEdges(VertexHelper vh, Color32 color32)
	{
		float innerWidth = halfWidth - edgeThickness;
		float innerHeight = halfHeight - edgeThickness;
		vh.AddVert(new Vector3(halfWidth + offsetWidth, offsetHeight, 0f), color32, MapUV(new Vector2(1, 0.5f)));
		vh.AddVert(new Vector3(innerWidth + offsetWidth, offsetHeight, 0f), color32, MapUV(new Vector2(innerWidth / halfWidth, 0.5f)));

		int triIdx = 2;
		for (int i = 1; i < segment; i++, triIdx += 2)
		{
			float radians = i * segmentRadians;
			float cos = Mathf.Cos(radians);
			float sin = Mathf.Sin(radians);
			Vector2 uvPosOuter = MapUV(new Vector2(cos * 0.5f + 0.5f, sin * 0.5f + 0.5f));
			Vector2 uvPosInner = MapUV(new Vector2(cos * (innerWidth / halfWidth) * 0.5f + 0.5f, sin * (innerHeight / halfHeight) * 0.5f + 0.5f));

			vh.AddVert(new Vector3(cos * halfWidth + offsetWidth, sin * halfHeight + offsetHeight, 0f), color32, uvPosOuter);
			vh.AddVert(new Vector3(cos * innerWidth + offsetWidth, sin * innerHeight + offsetHeight, 0f), color32, uvPosInner);

			vh.AddTriangle(triIdx, triIdx - 2, triIdx - 1);
			vh.AddTriangle(triIdx, triIdx - 1, triIdx + 1);
		}
		vh.AddTriangle(0, triIdx - 2, triIdx - 1);
		vh.AddTriangle(0, triIdx - 1, 1);
	}

	private Vector2 MapUV(Vector2 uv)
	{
		if (sprite == null)
		{
			return uv;
		}

		Rect rect = sprite.textureRect;
		Rect textureRect = new Rect(
			rect.x / sprite.texture.width,
			rect.y / sprite.texture.height,
			rect.width / sprite.texture.width,
			rect.height / sprite.texture.height
		);

		return new Vector2(
			Mathf.Lerp(textureRect.xMin, textureRect.xMax, uv.x),
			Mathf.Lerp(textureRect.yMin, textureRect.yMax, uv.y)
		);
	}
}
