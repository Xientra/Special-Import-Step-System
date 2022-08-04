using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public class SpecialImportProjectView
{
	private static readonly float underlineWidth = 1.0f;

	public const float UNITY_ICON_SIZE = 18;
	public const float UNITY_LETTER_SIZE_APROX = 7;

	public static bool isActive = true;

	// ----- sub asset visualization ----- //

	public static bool visualizeSubAssets = false;
	private static string lastGUID = ""; // used to not visualize sub assets

	/// <summary>
	/// Ok so sub assets can be somewhat hacky be identified by order.
	/// By remembering how often in a row i got the same guid i know at which sub asset i am now.
	/// The first time i get a guid must then be -1 because it is the parent asset itself.
	/// </summary>
	private static int subAssetIndex = 0;

	static SpecialImportProjectView()
	{
		EditorApplication.projectWindowItemOnGUI += DrawSpecialImportStepView;
	}

	private static void DrawSpecialImportStepView(string guid, Rect selectionRect)
	{
		if (SISSData.Instance.drawInProjectView == false)
			return;

		if (visualizeSubAssets == false && guid == lastGUID)
			return;

		string assetPath = AssetDatabase.GUIDToAssetPath(guid);
		if (string.IsNullOrEmpty(assetPath))
			return;

		Type assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);

		// LoadAllAssetsAtPath is illigal to use on scene assets so specifically catch that case
		if (assetType == typeof(SceneAsset))
			return;

		// ----- get steps ----- //

		bool folderSteps = false;
		List<SpecialImportStep> steps;

		if (AssetDatabase.IsValidFolder(assetPath))
		{
			steps = SISSData.Instance.GetStepsForFolder(assetPath);
			folderSteps = true;
		}
		else
		{
			Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

			List<Type> types = new List<Type>(new HashSet<Type>(new List<Object>(subAssets).FindAll(sa => sa != null).ConvertAll(sa => sa.GetType())));
			types.Add(assetType);
			steps = SISSData.Instance.GetStepsForAsset(guid, assetPath, types.ToArray());

			// filter steps based on this sub asset type
			if (guid == lastGUID)
			{
				subAssetIndex++;
				steps = steps.FindAll(sis => sis.target.CheckType(subAssets[subAssetIndex].GetType()));
			}
			else
				subAssetIndex = -1;
		}

		// ----- draw ----- // 
		if (steps.Count > 0)
		{
			Rect r = selectionRect;

			// tooltip
			if (SISSData.Instance.showTooltip)
			{
				string tooltip = "";
				for (int i = 0; i < steps.Count; i++)
				{
					tooltip += steps[i].ToString() + (i == steps.Count - 1 ? "" : "\n");
					if (i != steps.Count - 1)
						tooltip += "\t\u2193\n"; // tab + down arrow + line break
				}

				EditorGUI.LabelField(r, new GUIContent("", tooltip));
			}

			Color c = folderSteps ? SISSData.Instance.pathStepColor : SISSData.Instance.guidStepColor;

			// icon
			SISSUtils.GUI.DrawProjectViewIcon(new Rect(r.x + r.width - r.height, r.y, r.height, r.height), SISSData.Instance.iconColor);

			// underline
			EditorGUI.DrawRect(new Rect(r.x + UNITY_ICON_SIZE, r.y + r.height - (underlineWidth / 2), r.width - UNITY_ICON_SIZE, underlineWidth), c);
			//float textPixelWidth = System.IO.Path.GetFileNameWithoutExtension(assetPath).Length * UNITY_LETTER_SIZE_APROX;
			//EditorGUI.DrawRect(new Rect(r.x + UNITY_ICON_SIZE, r.y + r.height - (underlineWidth / 2), textPixelWidth, underlineWidth), c);

			// overlay
			c.a = SISSData.Instance.overlayOpacity;
			EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, r.height), c);

			lastGUID = guid;
		}

		// repaint?
	}
}
