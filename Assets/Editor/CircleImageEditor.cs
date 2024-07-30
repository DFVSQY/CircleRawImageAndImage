using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(CircleImage), true)]
[CanEditMultipleObjects]
public class CircleImageEditor : ImageEditor
{
	SerializedProperty segmentProp;
	SerializedProperty fillModeProp;
	SerializedProperty edgeThicknessProp;

	protected override void OnEnable()
	{
		base.OnEnable();
		segmentProp = serializedObject.FindProperty("segment");
		fillModeProp = serializedObject.FindProperty("fillMode");
		edgeThicknessProp = serializedObject.FindProperty("edgeThickness");
	}

	public override void OnInspectorGUI()
	{
		// 调用基类方法显示基础属性
		base.OnInspectorGUI();

		Image image = target as Image;
		if (image.type != Image.Type.Simple) return;

		// 更新序列化对象
		serializedObject.Update();

		// 添加分割线
		EditorGUILayout.Space();
		EditorGUILayout.LabelField(string.Empty, GUI.skin.horizontalSlider);
		EditorGUILayout.Space();

		// 显示自定义属性
		EditorGUILayout.PropertyField(segmentProp);
		EditorGUILayout.PropertyField(fillModeProp);

		// 不同填充模式限制不同最小段数
		CircleImage.FillMode mode = (CircleImage.FillMode)fillModeProp.enumValueIndex;
		int minSegment = mode == CircleImage.FillMode.FillOutside ? 4 : 3;
		segmentProp.intValue = Mathf.Max(minSegment, segmentProp.intValue);

		// 根据 mode 的值决定是否显示 edgeThickness 属性
		if (mode == CircleImage.FillMode.Edge)
		{
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(edgeThicknessProp);
			EditorGUI.indentLevel--;
		}

		// 应用修改后的属性
		serializedObject.ApplyModifiedProperties();
	}
}
