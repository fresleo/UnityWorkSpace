#if UNITY_EDITOR

//Scene与Game视图同步，方便美术调整查看效果
using System;
using UnityEngine;

public class AlignToEditorCamera : MonoBehaviour
{
	public bool autoAlignPosition = true;
	public bool autoAlignRotation = true;

	// 添加这个方法是为了能够在 Editor 中控制 enable 开关
	private void OnEnable()
	{
	}
}

#endif // UNITY_EDITOR
