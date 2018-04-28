using System;
using System.IO;
using UnityEngine;
using UnityEditor;

class AnimationClipSettings
{
	SerializedProperty m_Property;

	private SerializedProperty Get (string property) { return m_Property.FindPropertyRelative(property); }

	public AnimationClipSettings(SerializedProperty prop) { m_Property = prop; }
		
	public float startTime   { get { return Get("m_StartTime").floatValue; } set { Get("m_StartTime").floatValue = value; } }
	public float stopTime	{ get { return Get("m_StopTime").floatValue; }  set { Get("m_StopTime").floatValue = value; } }
	public float orientationOffsetY { get { return Get("m_OrientationOffsetY").floatValue; } set { Get("m_OrientationOffsetY").floatValue = value; } }
	public float level { get { return Get("m_Level").floatValue; } set { Get("m_Level").floatValue = value; } }
	public float cycleOffset { get { return Get("m_CycleOffset").floatValue; } set { Get("m_CycleOffset").floatValue = value; } }

	public bool loopTime { get { return Get("m_LoopTime").boolValue; } set { Get("m_LoopTime").boolValue = value; } }
	public bool loopBlend { get { return Get("m_LoopBlend").boolValue; } set { Get("m_LoopBlend").boolValue = value; } }
	public bool loopBlendOrientation { get { return Get("m_LoopBlendOrientation").boolValue; } set { Get("m_LoopBlendOrientation").boolValue = value; } }
	public bool loopBlendPositionY { get { return Get("m_LoopBlendPositionY").boolValue; } set { Get("m_LoopBlendPositionY").boolValue = value; } }
	public bool loopBlendPositionXZ { get { return Get("m_LoopBlendPositionXZ").boolValue; } set { Get("m_LoopBlendPositionXZ").boolValue = value; } }
	public bool keepOriginalOrientation { get { return Get("m_KeepOriginalOrientation").boolValue; } set { Get("m_KeepOriginalOrientation").boolValue = value; } }
	public bool keepOriginalPositionY { get { return Get("m_KeepOriginalPositionY").boolValue; } set { Get("m_KeepOriginalPositionY").boolValue = value; } }
	public bool keepOriginalPositionXZ { get { return Get("m_KeepOriginalPositionXZ").boolValue; } set { Get("m_KeepOriginalPositionXZ").boolValue = value; } }
	public bool heightFromFeet { get { return Get("m_HeightFromFeet").boolValue; } set { Get("m_HeightFromFeet").boolValue = value; } }
	public bool mirror { get { return Get("m_Mirror").boolValue; } set { Get("m_Mirror").boolValue = value; } }
}

class EditAnimationClipSettings
{
	[MenuItem("Mecanim/Edit Clip Settings")]
	static void DoEditClipSettings()
	{
		AnimationClip clip = Selection.activeObject as AnimationClip;
		if (clip != null)
		{
			SerializedObject serializedClip = new SerializedObject(clip);
			AnimationClipSettings clipSettings = new AnimationClipSettings(serializedClip.FindProperty("m_AnimationClipSettings"));

			clipSettings.loopTime = !clipSettings.loopTime;

			serializedClip.ApplyModifiedProperties();
		}
	}
}
