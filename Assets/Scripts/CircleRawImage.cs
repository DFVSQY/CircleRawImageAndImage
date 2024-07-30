using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Circle RawImage", 12)]
public class CircleRawImage : RawImage
{
	public enum FillMode
	{
		FillInside,
		FillOutside,
		Edge
	};

	/// <summary>
	/// 定义了圆形分割成多少段，用于控制圆的细腻程度。
	/// </summary>
	public int segment = 50;

	public FillMode fillMode;

	/// <summary>
	/// 边缘厚度，仅在 Edge 模式下有效。
	/// </summary>
	public float edgeThickness = 10f;

	/// <summary>
	/// 用于存储绘制区域的一半宽度和高度。
	/// </summary>
	private float halfWidth, halfHeight;

	/// <summary>
	/// 圆的中心点相对支点的偏移
	/// </summary>
	private float offsetWidth, offsetHeight;

	/// <summary>
	/// 单段圆弧的弧度，用于计算顶点位置。
	/// </summary>
	private float segmentRadians;

	protected override void OnPopulateMesh(VertexHelper vh)
	{
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
			// 分成四个象限，计算每个象限的段数
			int quarterSegment = Mathf.CeilToInt(segment / 4);

			// 每个象限分隔段对应的弧度
			segmentRadians = 360f / (quarterSegment * 4) * Mathf.Deg2Rad;

			// 添加矩形的四个顶点
			vh.AddVert(new Vector3(halfWidth + offsetWidth, halfHeight + offsetHeight, 0f), color32, MapUV(new Vector2(1, 1)));     // 右上
			vh.AddVert(new Vector3(-halfWidth + offsetWidth, halfHeight + offsetHeight, 0f), color32, MapUV(new Vector2(0, 1)));    // 左上
			vh.AddVert(new Vector3(-halfWidth + offsetWidth, -halfHeight + offsetHeight, 0f), color32, MapUV(new Vector2(0, 0)));   // 左下
			vh.AddVert(new Vector3(halfWidth + offsetWidth, -halfHeight + offsetHeight, 0f), color32, MapUV(new Vector2(1, 0)));    // 右下

			// 初始化为4，因为前面已经添加了4个顶点。
			int triIdx = 4;

			// 填充四个象限
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
		// 添加圆形顶点
		vh.AddVert(new Vector3(offsetWidth, offsetHeight, 0f), color32, MapUV(new Vector2(0.5f, 0.5f)));

		// 添加圆上的第一个顶点，它位于右侧顶点（0度）
		vh.AddVert(new Vector3(halfWidth + offsetWidth, offsetHeight, 0f), color32, MapUV(new Vector2(1, 0.5f)));

		// 用于记录当前处理的顶点索引，从2开始，因为前两个顶点已经添加了
		int triIdx = 2;
		for (int i = 1; i < segment; i++, triIdx++)
		{
			// 每一段的弧度
			float radians = i * segmentRadians;

			// 将弧度转换为UV坐标
			Vector2 uvPos = MapUV(new Vector2(Mathf.Cos(radians) * 0.5f + 0.5f, Mathf.Sin(radians) * 0.5f + 0.5f));

			// 添加当前顶点
			vh.AddVert(new Vector3(Mathf.Cos(radians) * halfWidth + offsetWidth, Mathf.Sin(radians) * halfHeight + offsetHeight, 0f), color32, uvPos);

			// 添加三角形（由当前顶点、前一个圆周顶点和圆心构成）
			vh.AddTriangle(triIdx, triIdx - 1, 0);
		}

		// 添加最后一个三角形（连接起始顶点和终点，使圆闭合）
		vh.AddTriangle(1, triIdx - 1, 0);
	}

	private void FillOutside(VertexHelper vh, Vector3 initialPoint, int quarterIndex, int quarterSegment, ref int triIdx, Color32 color32)
	{
		int startIdx = quarterIndex * quarterSegment;
		int endIdx = (quarterIndex + 1) * quarterSegment;

		// 将初始顶点添加到顶点列表中
		vh.AddVert(initialPoint, color32, MapUV(new Vector2(0.5f + (initialPoint.x - offsetWidth) / halfWidth * 0.5f, 0.5f + (initialPoint.y - offsetHeight) / halfHeight * 0.5f)));
		triIdx++;

		// 填充象限每个弧度的顶点
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

		// 添加起始的两个顶点，右侧外圈点和右侧内圈点
		vh.AddVert(new Vector3(halfWidth + offsetWidth, offsetHeight, 0f), color32, MapUV(new Vector2(1, 0.5f)));
		vh.AddVert(new Vector3(innerWidth + offsetWidth, offsetHeight, 0f), color32, MapUV(new Vector2(innerWidth / halfWidth, 0.5f)));

		int triIdx = 2;

		// 计算每个分隔段的顶点
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

		// 补全最后的分隔段顶点
		vh.AddTriangle(0, triIdx - 2, triIdx - 1);
		vh.AddTriangle(0, triIdx - 1, 1);
	}

	private Vector2 MapUV(Vector2 uv)
	{
		return new Vector2(uvRect.x + uv.x * uvRect.width, uvRect.y + uv.y * uvRect.height);
	}
}
