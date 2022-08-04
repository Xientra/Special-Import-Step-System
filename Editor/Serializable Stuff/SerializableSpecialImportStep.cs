using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[Serializable]
public class SerializableSpecialImportStep
{
	[SerializeField]
	public string classType;
	[SerializeField]
	public SpecialImportTarget target;
	[SerializeField]
	public string[] parameters;

	public SerializableSpecialImportStep(string classType, SpecialImportTarget target, string[] parameters)
	{
		this.classType = classType;
		this.target = target;
		this.parameters = parameters;
	}

	public SerializableSpecialImportStep(SpecialImportStep step)
	{
		SerializeStepWithReflections(step);
	}

	public SpecialImportStep DeserializeStep()
	{
		return DeserializeWithReflections();
	}

	/// <summary>
	/// Gets the desired list of fields of a type for this serialization / deserialization.
	/// </summary>
	private System.Reflection.FieldInfo[] UniformGetFields(Type type)
	{
		// get all variables from the class that step is a object from (not necessarily SpecialImportStep if step is the object of a subclass of it)
		// filter out all variables that are not public. Ideally this will later only keep serializable varaibles.
		// finally sort by MetadataToken to always ensure the same order or fields
		return type.GetFields()
			.Where(field => field.IsPublic && field.IsStatic == false)
			// just to be save i only take specific property types
			.Where(field =>
				field.FieldType == typeof(string) ||
				field.FieldType == typeof(float) ||
				field.FieldType == typeof(bool) ||
				field.FieldType == typeof(int))
			.OrderBy(field => field.MetadataToken).ToArray();
	}

	/// <summary>
	/// Serializes a SpecialImportStep (and the subclasses of it ofc) to this SerializableSpecialImportStep.
	/// </summary>
	public void SerializeStepWithReflections(SpecialImportStep step)
	{
		System.Reflection.FieldInfo[] fields = UniformGetFields(step.GetType());

		this.target = step.target;

		string[] parameters = new string[fields.Length];

		for (int i = 0; i < fields.Length; i++)
		{
			parameters[i] = fields[i].GetValue(step).ToString();
		}

		this.parameters = parameters;
		classType = step.GetType().ToString();
	}

	private SpecialImportStep DeserializeWithReflections()
	{
		Type deserializedType = Type.GetType(classType);
		if (deserializedType == null || deserializedType.IsSubclassOf(typeof(SpecialImportStep)) == false)
		{
			Debug.LogError("DeserializeWithReflections could not deserialize type. It was of type " + deserializedType?.Name);
			return null;
		}

		// This is a dynamic constructor for all subclasses of SpecialImportStep. It will return a SpecialImportStep object that might also
		// be of a subclass of SpecialImportStep.
		// For this to work SpecialImportStep and all of its subclasses need a parameterless contructor.
		SpecialImportStep sis = (SpecialImportStep)Activator.CreateInstance(deserializedType);

		sis.target = this.target;

		System.Reflection.FieldInfo[] fields = UniformGetFields(deserializedType);

		for (int i = 0; i < fields.Length; i++)
		{
			// this is a dynamic cast that will work for all primitive types, likely not for more complicated stuff
			var converter = System.ComponentModel.TypeDescriptor.GetConverter(fields[i].FieldType);
			fields[i].SetValue(sis, converter.ConvertFrom(parameters[i]));
		}

		return sis;
	}

	[MenuItem("TestArea/Test Reflection Serialization", priority = 69)]
	public static void TestReflectionSerialization()
	{
		// create step
		SpecialImportRenameFromTo sis = new SpecialImportRenameFromTo(new SpecialImportTarget("some guid, i don't care"));
		sis.target.subAssetMatchString = "renameFrom";
		sis.renameTo = "renameTo";

		// create serialized version of step
		SerializableSpecialImportStep ssis = new SerializableSpecialImportStep(sis);

		// log
		Debug.Log("Serialized SpecialImportRenameFromTo\n" + ssis.parameters.ToString());

		// create deserialized step from serialized version
		SpecialImportStep deserializedSIS = ssis.DeserializeWithReflections();

		// log
		Debug.Log("Deserialized SpecialImportStep with type " + deserializedSIS.GetType() + "\n");
		Debug.Log("guid: " + deserializedSIS.ToString());

		SerializableSpecialImportStep ssis2 = new SerializableSpecialImportStep(deserializedSIS);
		for (int i = 0; i < ssis2.parameters.Length; i++)
			Debug.Log(ssis2.parameters[i]);
	}
}