using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class SpecialImportPostprocessMesh : SpecialImportStep
{
	public bool centerPivot = false;
	public bool alignPivot = false;
	public Vector3 alignment = new Vector3(0, -1, 0);
	public bool applyScale = false;
	public bool applyRotation = false;

	public SpecialImportPostprocessMesh() { }
	public SpecialImportPostprocessMesh(SpecialImportTarget target) : base(target) { }

	public override string ToString()
	{
		return "Postprocess Mesh";
	}

	public override string GetName()
	{
		return "Postprocess Mesh";
	}

	public override List<Type> GetWorksForTypesList()
	{
		return new List<Type>() { typeof(Mesh) };
	}

	public override bool DrawGUI(bool inCreation)
	{
		GUIBeginDefault(inCreation);
		{
			target.subAssetMatchString = SISSUtils.GUI.SyntaxHighlightTextField("Mesh Name:", target.subAssetMatchString, "Match string to target all meshes that are to be modified.");

			EditorGUILayout.Separator();

			centerPivot = EditorGUILayout.ToggleLeft(new GUIContent("Center Pivot", "Translates the mesh so it's pivot is at the mesh center."), centerPivot);
			if (centerPivot)
			{
				EditorGUI.indentLevel++;
				alignPivot = EditorGUILayout.ToggleLeft(new GUIContent("Align Pivot", "Moves the center of the mesh to the furthers point in the given direction. Very usefull for putting the center at the bottom when the mesh is a building or similar."), alignPivot);
				if (alignPivot)
				{
					EditorGUI.indentLevel++;
					alignment = EditorGUILayout.Vector3Field(new GUIContent("Alignment", "Describes where the mesh pivot should be placed. (0, -1, 0) for example means at the bottom of the mesh in world coordinates."), alignment);
					EditorGUI.indentLevel--;
				}
				EditorGUI.indentLevel--;
			}

			EditorGUILayout.Separator();

			applyScale = EditorGUILayout.ToggleLeft(new GUIContent("Apply Scale", "Applies the scale of the Model to the mesh."), applyScale);
			applyRotation = EditorGUILayout.ToggleLeft(new GUIContent("Apply Rotation", "Applies the rotation of the Model to the mesh."), applyRotation);
		}
		GUIEndDefault();

		return DrawAddRemoveGUI(inCreation);
	}

	public override void Apply(Object importObject, UnityEditor.AssetImporters.AssetImportContext context, AssetImporter assetImporter)
	{
		GameObject g = importObject as GameObject;

		int applyCount = 0;

		// apply for all skinned mesh renderer
		SkinnedMeshRenderer[] smrs = g.GetComponentsInChildren<SkinnedMeshRenderer>();
		for (int i = 0; i < smrs.Length; i++)
			if (target.IsSubAssetTarget(smrs[i].sharedMesh.name, context.assetPath, typeof(Mesh)))
			{
				ApplyToMesh(smrs[i].sharedMesh, smrs[i].gameObject);
				applyCount++;
			}

		// apply for all mesh filter
		MeshFilter[] mfs = g.GetComponentsInChildren<MeshFilter>();
		for (int i = 0; i < mfs.Length; i++)
			if (target.IsSubAssetTarget(mfs[i].sharedMesh.name, context.assetPath, typeof(Mesh)))
			{
				ApplyToMesh(mfs[i].sharedMesh, mfs[i].gameObject);
				applyCount++;
			}

		if (applyCount == 0)
			Debug.LogWarning("Could not find any mesh with a name matching: " + SISSUtils.ResolveKeywords(target.subAssetMatchString, context.assetPath, "", typeof(Mesh)) + " on Center Mesh Import step for asset " + TargetName);
		else
			Debug.Log("Applied Mesh Postprocessing to " + applyCount + " Meshes.");
	}

	private float AbsProjectLength(Vector3 vector, Vector3 onNormal)
	{
		vector.x = Mathf.Abs(vector.x);
		vector.y = Mathf.Abs(vector.y);
		vector.z = Mathf.Abs(vector.z);
		onNormal.x = Mathf.Abs(onNormal.x);
		onNormal.y = Mathf.Abs(onNormal.y);
		onNormal.z = Mathf.Abs(onNormal.z);

		return Vector3.Project(vector, onNormal).magnitude;
	}

	private void ApplyToMesh(Mesh targetMesh, GameObject meshParent)
	{
		if (targetMesh.vertices.Length == 0)
			return;

		// translation
		Vector3 translation = Vector3.zero;
		if (centerPivot)
		{
			translation -= targetMesh.bounds.center;
			meshParent.transform.localPosition += meshParent.transform.TransformVector(targetMesh.bounds.center); // maybe change this from localPositon to position if stuff breaks in the future
		}
		if (alignPivot)
		{
			Vector3 t = Vector3.Project(meshParent.transform.TransformVector(targetMesh.bounds.extents), alignment).magnitude * alignment.normalized;
			translation -= meshParent.transform.InverseTransformVector(t);
			meshParent.transform.position += t;
		}

		// rotation
		Quaternion rotation = Quaternion.identity;
		if (applyRotation)
		{
			rotation = meshParent.transform.localRotation;
			meshParent.transform.localRotation = Quaternion.identity;
		}

		// scale | apply scale whenever rotation should be applied. If scale should not be applied it is later reversed
		Vector3 scaling = Vector3.one;
		if (applyScale || applyRotation)
		{
			scaling = SISSUtils.AbsVector(meshParent.transform.localScale);
			if (applyScale)
				meshParent.transform.localScale = SISSUtils.SignVector(meshParent.transform.localScale);
		}

		Matrix4x4 translationMat = Matrix4x4.Translate(translation);
		Matrix4x4 inverseScaleMat = Matrix4x4.Inverse(Matrix4x4.Scale(scaling));
		Matrix4x4 rotationScaleMat = Matrix4x4.TRS(Vector3.zero, rotation, scaling);

		// this might not be neccessary, i get the same result without inverse + transpose
		Matrix4x4 normalMat = Matrix4x4.Transpose(Matrix4x4.Inverse(rotationScaleMat));
		Matrix4x4 inverseScaleNormalMat = Matrix4x4.Transpose(Matrix4x4.Inverse(inverseScaleMat));

		Vector3[] vertices = targetMesh.vertices;
		Vector3[] normals = targetMesh.normals;
		for (int i = 0; i < vertices.Length; i++)
		{
			vertices[i] = translationMat.MultiplyPoint(vertices[i]);

			vertices[i] = rotationScaleMat.MultiplyPoint(vertices[i]);
			normals[i] = normalMat.MultiplyVector(normals[i]);

			if (applyScale == false)
			{
				vertices[i] = inverseScaleMat.MultiplyPoint(vertices[i]);
				normals[i] = inverseScaleNormalMat.MultiplyVector(normals[i]);
			}
		}

		targetMesh.vertices = vertices;
		targetMesh.normals = normals;

		targetMesh.RecalculateBounds();
	}
}
