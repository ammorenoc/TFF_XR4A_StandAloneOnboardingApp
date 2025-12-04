using UnityEngine;
using UnityEditor;

namespace InnerDriveStudios.Util
{
	/**
     * Defines the editor for the Note component which is a simple sticky-note-like component,
     * through which you can document your scene.
     * 
     * @author J.C. Wichman - InnerDriveStudios.com
     */
	[CustomEditor(typeof(Note))]
	public class NoteEditor : Editor
	{
		private static string[] descriptions = { "Documentation", "Todo", "Nice to have", "Minor bug", "Critical bug" };

		private static Color[] colors = {
			new Color(1, 1, 0.4f),
			new Color(1, 0.75f, 0.3f),
			new Color(1, 0.5f, 0.2f),
			new Color(1, 0.25f, 0.1f),
			new Color(1, 0, 0)
		};

		private static GUIStyle[] noteStyles = null;

		private static GUILayoutOption[] TEXT_AREA_OPTIONS = {
			GUILayout.ExpandWidth (true),
			GUILayout.ExpandHeight(false)
		};

		//textures are unloaded on scene changes, so instead of a boolean we use an actual small texture
		//to detect if textures need to be reinitialized or not
		private static Texture textureInitialized = null;

		private static void GenerateBackgroundTexturesForDescriptions()
		{
			if (textureInitialized) return;

			noteStyles = new GUIStyle[descriptions.Length];

			for (int i = 0; i < descriptions.Length; i++)
			{
				Texture2D texture = new Texture2D(1, 1);
				texture.SetPixel(0, 0, colors[i]);
				texture.Apply();

				GUIStyle style = new GUIStyle();
				style.normal.background = texture;
				style.wordWrap = true;
				style.fontSize = 12;
				style.padding = new RectOffset(5, 5, 5, 5);
				style.margin = new RectOffset(5, 5, 5, 5);

				noteStyles[i] = style;
			}

			textureInitialized = new Texture2D(1, 1);
		}

		private SerializedProperty noteType;
		private SerializedProperty noteText;

		private void OnEnable()
		{
			GenerateBackgroundTexturesForDescriptions();

			noteType = serializedObject.FindProperty("noteType");
			noteText = serializedObject.FindProperty("noteText");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			noteType.intValue = EditorGUILayout.Popup(noteType.intValue, descriptions);
			noteText.stringValue = EditorGUILayout.TextArea(noteText.stringValue, noteStyles[noteType.intValue], TEXT_AREA_OPTIONS);

			serializedObject.ApplyModifiedProperties();
		}

	}

}