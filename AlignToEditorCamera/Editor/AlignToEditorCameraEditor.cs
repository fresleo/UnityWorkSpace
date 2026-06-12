//Scene与Game视图同步，方便美术调整查看效果

using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(AlignToEditorCamera))]
public class AlignToEditorCameraEditor : Editor
{
	public override void OnInspectorGUI()
	{
		base.DrawDefaultInspector();
	}

	public void OnSceneGUI()
	{
		AlignToEditorCamera camreaUtils = (AlignToEditorCamera)target;

		if (!camreaUtils.enabled)
			return;

		if (camreaUtils.autoAlignPosition)
		{
			camreaUtils.transform.position = SceneView.lastActiveSceneView.camera.transform.position;
		}

		if (camreaUtils.autoAlignRotation)
		{
			camreaUtils.transform.rotation = SceneView.lastActiveSceneView.camera.transform.rotation;
		}
	}
}

