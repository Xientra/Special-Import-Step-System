using System;
using UnityEditor;
using UnityEngine;

public class SimpleConfirmPopup : EditorWindow
{
	public static void Show(Action<bool> onClose, string title = "Confirm", string positiveBtnLabel = "Confirm", string negativeBtnLabel = "Cancel")
	{
		SimpleConfirmPopup window = ScriptableObject.CreateInstance<SimpleConfirmPopup>();
		window.label = title;
		window.onClose = onClose;
		window.positiveBtnLabel = positiveBtnLabel;
		window.negativeBtnLabel = negativeBtnLabel;

		window.position = new Rect(Screen.width / 2, Screen.height / 2, 250, 50);
		window.ShowPopup();
	}

	private Action<bool> onClose;
	private string label;

	private string positiveBtnLabel;
	private string negativeBtnLabel;

	void OnGUI()
	{
		EditorGUILayout.LabelField(label, EditorStyles.wordWrappedLabel);

		EditorGUILayout.Separator();

		EditorGUILayout.BeginHorizontal();
		{
			if (GUILayout.Button(positiveBtnLabel))
			{
				onClose.Invoke(true);
				this.Close();
			}

			if (GUILayout.Button(negativeBtnLabel))
			{
				onClose.Invoke(false);
				this.Close();
			}
		}
		EditorGUILayout.EndHorizontal();
	}
}
