using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(FadeBehaviour))]
public class FadeTrackDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int fieldCount = 3;
        
        SerializedProperty fadeTypeProperty = property.FindPropertyRelative("fadeType");
        if (fadeTypeProperty != null && fadeTypeProperty.enumValueIndex == 3)
        {
            SerializedProperty useCustomCurveProperty = property.FindPropertyRelative("useCustomCurve");
            if (useCustomCurveProperty != null && useCustomCurveProperty.boolValue)
            {
                fieldCount = 6;
            }
            else
            {
                fieldCount = 5;
            }
        }
        
        return fieldCount * EditorGUIUtility.singleLineHeight + (fieldCount - 1) * EditorGUIUtility.standardVerticalSpacing;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty fadeTypeProperty = property.FindPropertyRelative("fadeType");
        SerializedProperty fadeColorProperty = property.FindPropertyRelative("fadeColor");
        SerializedProperty startAlphaProperty = property.FindPropertyRelative("startAlpha");
        SerializedProperty endAlphaProperty = property.FindPropertyRelative("endAlpha");
        SerializedProperty useCustomCurveProperty = property.FindPropertyRelative("useCustomCurve");
        SerializedProperty fadeCurveProperty = property.FindPropertyRelative("fadeCurve");

        Rect singleFieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        
        EditorGUI.PropertyField(singleFieldRect, fadeTypeProperty);
        singleFieldRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        EditorGUI.PropertyField(singleFieldRect, fadeColorProperty);
        singleFieldRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        if (fadeTypeProperty.enumValueIndex == 3)
        {
            EditorGUI.PropertyField(singleFieldRect, startAlphaProperty);
            singleFieldRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            EditorGUI.PropertyField(singleFieldRect, endAlphaProperty);
            singleFieldRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            EditorGUI.PropertyField(singleFieldRect, useCustomCurveProperty);
            singleFieldRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            if (useCustomCurveProperty.boolValue)
            {
                EditorGUI.PropertyField(singleFieldRect, fadeCurveProperty);
            }
        }
    }
}
