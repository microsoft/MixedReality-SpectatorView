using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class RevertPropertyChangesScript : MonoBehaviour
{
#if UNITY_EDITOR
	private Dictionary<Material, Dictionary<RevertPropertyChanges, object>> materialValues = new Dictionary<Material, Dictionary<RevertPropertyChanges, object>>();

	private void Awake()
	{
		MonoBehaviour[] everythingInScene = FindObjectsOfType<MonoBehaviour>();

		// Get all objects
		for (int i = 0; i < everythingInScene.Length; ++i)
		{
			FieldInfo[] fields = everythingInScene[i].GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);

			// Get all fields
			foreach(var field in fields)
			{
				IEnumerable<RevertPropertyChanges> revertPropsAttrs = field.GetCustomAttributes<RevertPropertyChanges>();

				// For each RevertPropertyChanges attr on the field
				foreach (var revertPropsAttr in revertPropsAttrs)
				{
					if (field.FieldType == typeof(Material))
					{
						Material material = field.GetValue(everythingInScene[i]) as Material;
						if (!materialValues.ContainsKey(material))
						{
							materialValues.Add(material, new Dictionary<RevertPropertyChanges, object>());
						}

						// If we are not already storing the value
						if (!materialValues[material].ContainsKey(revertPropsAttr))
						{
							object originalValue = null;

							if (revertPropsAttr.TypeOfProperty == typeof(float))
							{
								originalValue = material.GetFloat(revertPropsAttr.PropertyName);
							} else if (revertPropsAttr.TypeOfProperty == typeof(Vector3))
							{
								originalValue = material.GetVector(revertPropsAttr.PropertyName);
							} else if (revertPropsAttr.TypeOfProperty == typeof(Color))
							{
								originalValue = material.GetColor(revertPropsAttr.PropertyName);
							}

							//Debug.LogErrorFormat("Storing property {0} on {1} to {2}", revertPropsAttr.PropertyName, material.name, originalValue);
							materialValues[material].Add(revertPropsAttr, originalValue);
						}
					} else if (field.FieldType == typeof(List<Material>) || field.FieldType == typeof(Material[]))
					{
						IEnumerable<Material> materials = field.GetValue(everythingInScene[i]) as IEnumerable<Material>;
						foreach (Material material in materials)
						{
							if (!materialValues.ContainsKey(material))
							{
								materialValues.Add(material, new Dictionary<RevertPropertyChanges, object>());
							}

							// If we are not already storing the value
							if (!materialValues[material].ContainsKey(revertPropsAttr))
							{
								object originalValue = null;

								if (revertPropsAttr.TypeOfProperty == typeof(float))
								{
									originalValue = material.GetFloat(revertPropsAttr.PropertyName);
								}
								else if (revertPropsAttr.TypeOfProperty == typeof(Vector3))
								{
									originalValue = material.GetVector(revertPropsAttr.PropertyName);
								}
								else if (revertPropsAttr.TypeOfProperty == typeof(Color))
								{
									originalValue = material.GetColor(revertPropsAttr.PropertyName);
								}

								//Debug.LogErrorFormat("Storing property {0} on {1} to {2}", revertPropsAttr.PropertyName, material.name, originalValue);
								materialValues[material].Add(revertPropsAttr, originalValue);
							}
						}
					}
				}
			}
		}
	}

	private void OnDestroy()
	{
		foreach (KeyValuePair<Material, Dictionary<RevertPropertyChanges, object>> matWithVals in materialValues)
		{
			foreach (KeyValuePair<RevertPropertyChanges, object> propertyWithValue in matWithVals.Value)
			{
				if (propertyWithValue.Key.TypeOfProperty == typeof(float))
				{
					matWithVals.Key.SetFloat(propertyWithValue.Key.PropertyName, (float)propertyWithValue.Value);
				}
				else if (propertyWithValue.Key.TypeOfProperty == typeof(Vector3))
				{
					// Even though we expose vector3, cast as a vector4 because that is
					// what shader uses
					matWithVals.Key.SetVector(propertyWithValue.Key.PropertyName, (Vector4)propertyWithValue.Value);
				}
				else if (propertyWithValue.Key.TypeOfProperty == typeof(Color))
				{
					matWithVals.Key.SetColor(propertyWithValue.Key.PropertyName, (Color)propertyWithValue.Value);
				}
			}
		}
	}
#endif
}
