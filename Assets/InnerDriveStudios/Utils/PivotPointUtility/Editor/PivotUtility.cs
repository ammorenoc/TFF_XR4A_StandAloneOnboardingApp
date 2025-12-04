using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace InnerDriveStudios.Util
{
	public class PivotUtility
	{

		[MenuItem(Settings.MENU_PATH + "Bottom Center Pivot")]
		private static void BottomCenterPivot()
		{
			if (Common.IsInPlayMode())
			{
				EditorUtility.DisplayDialog("Error", Strings.ONLY_IN_EDITMODE, Strings.OK);
				return;
			}

			if (Common.GetSelectedObjectsCount() != 1)
			{
				EditorUtility.DisplayDialog("Error", Strings.ONLY_ONE_ITEM, Strings.OK);
				return;
			}

			if (!Common.IsRegularTransform(Selection.activeTransform))
			{
				EditorUtility.DisplayDialog("Error", Strings.NOT_REGULAR_TRANSFORM, Strings.OK);
				return;
			}

			if (!Common.IsRootTransform(Selection.activeTransform))
			{
				EditorUtility.DisplayDialog("Error", Strings.NOT_ROOT_TRANSFORM, Strings.OK);
				return;
			}

			if (Common.HasMeshRenderer(Selection.activeTransform))
			{
				EditorUtility.DisplayDialog("Error", Strings.NO_MESHRENDERER, Strings.OK);
				return;
			}


			/*
			if (!Common.IsNonPrefabGameObjectInstance(Selection.activeGameObject))
			{
				EditorUtility.DisplayDialog("Error", Strings.NOT_SIMPLE_GAMEOBJECT_INSTANCE, Strings.OK);
				return;
			}
			*/

			ProcessTransform(Selection.activeTransform);
		}

		private static void ProcessTransform(Transform selectedTransform)
		{
			if (!Common.GetWorldBounds(selectedTransform, out Bounds bounds)) return;

			//make sure we can undo everything
			Undo.RecordObject(selectedTransform, "Pivot point fix");
			List<Transform> children = new List<Transform>();
			foreach (Transform child in selectedTransform)
			{
				Undo.RecordObject(child, "Pivot point fix");
				children.Add(child);
			}

			//move pivot point down
			Vector3 originalPosition = selectedTransform.position;
			selectedTransform.position = bounds.center + Vector3.down * bounds.extents.y;

			//move all objects in the transform up
			Vector3 delta = originalPosition - selectedTransform.position;
			foreach (Transform child in children) child.position += delta;
		}
	}
}

