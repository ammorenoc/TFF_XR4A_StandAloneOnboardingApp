using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace InnerDriveStudios.Util
{
    /**
     * Bunch of utility methods to make the other classes simpler.
     * 
     * @author J.C.Wichman - InnerDriveStudios.com
     */
    public static class Common
	{
		///note that you can be in both play mode and one of these edit modes, since you can edit in play mode
		public enum EditMode { Normal, Prefab };

		/// <summary>
		/// Returns whether the application is in play mode
		/// </summary>
		/// <returns></returns>
		public static bool IsInPlayMode()
		{
			return Application.isPlaying;
		}

		/// <summary>
		/// Returns the amount of selected items in the hierarchy, project window, etc.
		/// </summary>
		/// <returns></returns>
		public static int GetSelectedObjectsCount()
		{
			return Selection.objects.Length;
		}

		/// <summary>
		/// Are we in normal edit mode (not editing a prefab), or any sort of prefab edit mode.
		/// </summary>
		/// <returns></returns>
		public static EditMode GetEditMode()
		{
			return PrefabStageUtility.GetCurrentPrefabStage() == null ? EditMode.Normal : EditMode.Prefab;
		}

		/// <summary>
		/// Returns true is the GameObject is not null and in a scene, but not (part of) a prefab instance.
		/// Note that this also returns true for any non prefab parts in prefab edit mode.
		/// If you want to exclude prefab edit mode, include the GetEditMode into your test.
		/// </summary>
		/// <param name="pGameObject"></param>
		/// <returns></returns>
		public static bool IsNonPrefabGameObjectInstance(GameObject pGameObject)
		{
			return pGameObject != null && pGameObject.scene.IsValid() && !PrefabUtility.IsPartOfAnyPrefab(pGameObject);
		}

		public static bool IsPrefab (GameObject pGameObject)
		{
			return pGameObject != null && !pGameObject.scene.IsValid();
		}

		/// <summary>
		/// Is the transform a regular transform and not a RectTransform?
		/// </summary>
		/// <param name="pTransform"></param>
		/// <returns></returns>
		public static bool IsRegularTransform(Transform pTransform)
		{
			return pTransform != null && !(pTransform is RectTransform);
		}

		public static bool IsRootTransform(Transform pTransform)
		{
			return pTransform.parent == null;
		}

		public static bool IsEmpty(Transform pTransform)
		{
			return pTransform.GetComponents<Component>().Length == 1;
		}

		public static bool HasMeshRenderer(Transform pTransform)
		{
			return pTransform.GetComponent<MeshRenderer>() != null;
		}

		/// <summary> Returns false when the gameobject or its children do not contain any meshrenderer components</summary>
		public static bool GetWorldBounds(Transform pRoot, out Bounds bounds)
		{
			MeshRenderer[] meshRenderers = pRoot.GetComponentsInChildren<MeshRenderer>();

			if (meshRenderers.Length == 0)
			{
				bounds = new Bounds();
				return false;
			}

			//This gets the world bounds of every MeshRenderer and makes sure to return ONE BIG
			//bounds object that encapsulates all of them

			bounds = meshRenderers[0].bounds;
			for (int i = 1; i < meshRenderers.Length; i++)
			{
				bounds.Encapsulate(meshRenderers[i].bounds);
			}

			return true;
		}

        public static bool GetLocalBounds(Transform pRoot, out Bounds bounds)
        {
            MeshRenderer[] meshRenderers = pRoot.GetComponentsInChildren<MeshRenderer>();

            if (meshRenderers.Length == 0)
            {
                bounds = new Bounds();
                return false;
            }

			//Ok, so what we do here is we get the local bounds of every object... however...
			//We want to know the location and extends of these bounds in relation to the passed in pRoot
			//So we need to reinterpret the bounds local to meshRenderer[i] and configure out what those
			//bounds are relative to the root transform.

			bounds = ReinterpretBounds(pRoot, meshRenderers[0].transform, meshRenderers[0].localBounds);

            for (int i = 1; i < meshRenderers.Length; i++)
            {
                bounds.Encapsulate(
                    ReinterpretBounds(pRoot, meshRenderers[i].transform, meshRenderers[i].localBounds)
				);
            }

            return true;
        }

		private static Bounds ReinterpretBounds (Transform pParentSpace, Transform pOriginalLocalBoundsSpace, Bounds pBounds)
		{
			//First reinterpret the original center point, from local to world, world to parent (root)
			pBounds.center = pOriginalLocalBoundsSpace.TransformPoint(pBounds.center);
			pBounds.center = pParentSpace.InverseTransformPoint(pBounds.center);

            //Then reinterpret the bounds extents vector, from local to world, world to parent (root)
            pBounds.extents = pOriginalLocalBoundsSpace.TransformVector(pBounds.extents);
            pBounds.extents = pParentSpace.InverseTransformVector(pBounds.extents);
			return pBounds;
        }

        public static bool IsPartOfPrefabInstanceButNotRoot(GameObject obj)
        {
            // Check if the object is part of a prefab instance
            if (PrefabUtility.IsPartOfPrefabInstance(obj))
            {
                // Get the root of the prefab instance
                GameObject prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(obj);

                // If the object is not the root, then it's part of the prefab but not the root
                return prefabRoot != obj;
            }

            // The object is not part of a prefab instance
            return false;
        }

    }
}
