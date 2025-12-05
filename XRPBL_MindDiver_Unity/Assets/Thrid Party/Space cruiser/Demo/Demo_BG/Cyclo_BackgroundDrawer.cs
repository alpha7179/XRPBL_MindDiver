using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;

namespace cycl
{
    [CustomPropertyDrawer(typeof(Behavior.Cyclo_BackgroundBehaviour))]
    public class Cyclo_BackgroundDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int fieldCount = 4;
            return fieldCount * EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty _CycloBottomColorProp = property.FindPropertyRelative("_CycloBottomColor");
            SerializedProperty _CycloTopColorProp = property.FindPropertyRelative("_CycloTopColor");
            SerializedProperty _CycloHorizonOriginProp = property.FindPropertyRelative("_CycloHorizonOrigin");
            SerializedProperty _CycloGradiantSpreadProp = property.FindPropertyRelative("_CycloGradiantSpread");

            Rect singleFieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(singleFieldRect, _CycloBottomColorProp);

            singleFieldRect.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(singleFieldRect, _CycloTopColorProp);

            singleFieldRect.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(singleFieldRect, _CycloHorizonOriginProp);

            singleFieldRect.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(singleFieldRect, _CycloGradiantSpreadProp);
        }
    }
}
