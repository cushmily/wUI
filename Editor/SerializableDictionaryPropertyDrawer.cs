#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;

public abstract class SerializableDictionaryPropertyDrawer : PropertyDrawer
{
    private const string KeysFieldName = "_keys";
    private const string ValuesFieldName = "_values";

    private static GUIContent m_iconPlus = IconContent("Toolbar Plus", "Add entry");
    private static GUIContent m_iconMinus = IconContent("Toolbar Minus", "Remove entry");

    private static GUIContent m_warningIconConflict =
        IconContent("console.warnicon.sml", "Conflicting key, this entry will be lost");

    private static readonly GUIContent m_warningIconOther = IconContent("console.infoicon.sml", "Conflicting key");

    private static readonly GUIContent m_warningIconNull =
        IconContent("console.warnicon.sml", "Null key, this entry will be lost");

    private static readonly GUIStyle m_buttonStyle = GUIStyle.none;

    private object _mConflictKey = null;
    private object _mConflictValue = null;
    private int _mConflictIndex = -1;
    private int _mConflictOtherIndex = -1;
    private bool _mConflictKeyPropertyExpanded = false;
    private bool _mConflictValuePropertyExpanded = false;
    private float _mConflictLineHeight = 0f;

    private enum Action
    {
        None,
        Add,
        Remove
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        label = EditorGUI.BeginProperty(position, label, property);

        Action buttonAction = Action.None;
        int buttonActionIndex = 0;

        var keyArrayProperty = property.FindPropertyRelative(KeysFieldName);
        var valueArrayProperty = property.FindPropertyRelative(ValuesFieldName);

        if (_mConflictIndex != -1)
        {
            keyArrayProperty.InsertArrayElementAtIndex(_mConflictIndex);
            var keyProperty = keyArrayProperty.GetArrayElementAtIndex(_mConflictIndex);
            SetPropertyValue(keyProperty, _mConflictKey);
            keyProperty.isExpanded = _mConflictKeyPropertyExpanded;

            valueArrayProperty.InsertArrayElementAtIndex(_mConflictIndex);
            var valueProperty = valueArrayProperty.GetArrayElementAtIndex(_mConflictIndex);
            SetPropertyValue(valueProperty, _mConflictValue);
            valueProperty.isExpanded = _mConflictValuePropertyExpanded;
        }

        var buttonWidth = m_buttonStyle.CalcSize(m_iconPlus).x;

        var labelPosition = position;
        labelPosition.height = EditorGUIUtility.singleLineHeight;
        if (property.isExpanded)
            labelPosition.xMax -= m_buttonStyle.CalcSize(m_iconPlus).x;

        EditorGUI.PropertyField(labelPosition, property, label, false);
        // property.isExpanded = EditorGUI.Foldout(labelPosition, property.isExpanded, label);
        if (property.isExpanded)
        {
            var buttonPosition = position;
            buttonPosition.xMin = buttonPosition.xMax - buttonWidth;
            buttonPosition.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.BeginDisabledGroup(_mConflictIndex != -1);
            if (GUI.Button(buttonPosition, m_iconPlus, m_buttonStyle))
            {
                buttonAction = Action.Add;
                buttonActionIndex = keyArrayProperty.arraySize;
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.indentLevel++;
            var linePosition = position;
            linePosition.y += EditorGUIUtility.singleLineHeight;
            linePosition.xMax -= buttonWidth;

            foreach (var entry in EnumerateEntries(keyArrayProperty, valueArrayProperty))
            {
                var keyProperty = entry.keyProperty;
                var valueProperty = entry.valueProperty;
                int i = entry.index;

                float lineHeight = DrawKeyValueLine(keyProperty, valueProperty, linePosition, i);

                buttonPosition = linePosition;
                buttonPosition.x = linePosition.xMax;
                buttonPosition.height = EditorGUIUtility.singleLineHeight;
                if (GUI.Button(buttonPosition, m_iconMinus, m_buttonStyle))
                {
                    buttonAction = Action.Remove;
                    buttonActionIndex = i;
                }

                if (i == _mConflictIndex && _mConflictOtherIndex == -1)
                {
                    var iconPosition = linePosition;
                    iconPosition.size = m_buttonStyle.CalcSize(m_warningIconNull);
                    GUI.Label(iconPosition, m_warningIconNull);
                }
                else if (i == _mConflictIndex)
                {
                    var iconPosition = linePosition;
                    iconPosition.size = m_buttonStyle.CalcSize(m_warningIconConflict);
                    GUI.Label(iconPosition, m_warningIconConflict);
                }
                else if (i == _mConflictOtherIndex)
                {
                    var iconPosition = linePosition;
                    iconPosition.size = m_buttonStyle.CalcSize(m_warningIconOther);
                    GUI.Label(iconPosition, m_warningIconOther);
                }


                linePosition.y += lineHeight;
            }

            EditorGUI.indentLevel--;
        }

        if (buttonAction == Action.Add)
        {
            keyArrayProperty.InsertArrayElementAtIndex(buttonActionIndex);
            valueArrayProperty.InsertArrayElementAtIndex(buttonActionIndex);
        }
        else if (buttonAction == Action.Remove)
        {
            DeleteArrayElementAtIndex(keyArrayProperty, buttonActionIndex);
            DeleteArrayElementAtIndex(valueArrayProperty, buttonActionIndex);
        }

        _mConflictKey = null;
        _mConflictValue = null;
        _mConflictIndex = -1;
        _mConflictOtherIndex = -1;
        _mConflictLineHeight = 0f;
        _mConflictKeyPropertyExpanded = false;
        _mConflictValuePropertyExpanded = false;

        foreach (var entry1 in EnumerateEntries(keyArrayProperty, valueArrayProperty))
        {
            var keyProperty1 = entry1.keyProperty;
            var i = entry1.index;
            var keyProperty1Value = GetPropertyValue(keyProperty1);

            if (keyProperty1Value == null)
            {
                var valueProperty1 = entry1.valueProperty;
                SaveProperty(keyProperty1, valueProperty1, i, -1);
                DeleteArrayElementAtIndex(valueArrayProperty, i);
                DeleteArrayElementAtIndex(keyArrayProperty, i);

                break;
            }


            foreach (var entry2 in EnumerateEntries(keyArrayProperty, valueArrayProperty, i + 1))
            {
                var keyProperty2 = entry2.keyProperty;
                var j = entry2.index;
                var keyProperty2Value = GetPropertyValue(keyProperty2);

                if (!Equals(keyProperty1Value, keyProperty2Value))
                {
                    continue;
                }

                var valueProperty2 = entry2.valueProperty;
                SaveProperty(keyProperty2, valueProperty2, j, i);
                DeleteArrayElementAtIndex(keyArrayProperty, j);
                DeleteArrayElementAtIndex(valueArrayProperty, j);

                goto breakLoops;
            }
        }
        breakLoops:

        EditorGUI.EndProperty();
    }

    protected abstract float DrawKeyValueLine(SerializedProperty keyProperty, SerializedProperty valueProperty,
        Rect linePosition, int index);

    protected abstract float GetKeyValueLinePropertyHeight(float keyPropertyHeight, float valuePropertyHeight);

    protected virtual void DrawKeyProperty(SerializedProperty keyProperty, Rect keyPosition, GUIContent label)
    {
        EditorGUI.PropertyField(keyPosition, keyProperty, label, true);
    }

    protected virtual float GetKeyPropertyHeight(SerializedProperty keyProperty)
    {
        return EditorGUI.GetPropertyHeight(keyProperty);
    }

    protected virtual void DrawValueProperty(SerializedProperty valueProperty, Rect valuePosition, GUIContent label)
    {
        EditorGUI.PropertyField(valuePosition, valueProperty, label, true);
    }

    protected virtual float GetValuePropertyHeight(SerializedProperty valueProperty)
    {
        return EditorGUI.GetPropertyHeight(valueProperty);
    }

    private void SaveProperty(SerializedProperty keyProperty, SerializedProperty valueProperty, int index,
        int otherIndex)
    {
        _mConflictKey = GetPropertyValue(keyProperty);
        _mConflictValue = GetPropertyValue(valueProperty);
        var keyPropertyHeight = GetKeyPropertyHeight(keyProperty);
        var valuePropertyHeight = GetValuePropertyHeight(valueProperty);
        var lineHeight = GetKeyValueLinePropertyHeight(keyPropertyHeight, valuePropertyHeight);
        _mConflictLineHeight = lineHeight;
        _mConflictIndex = index;
        _mConflictOtherIndex = otherIndex;
        _mConflictKeyPropertyExpanded = keyProperty.isExpanded;
        _mConflictValuePropertyExpanded = valueProperty.isExpanded;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var propertyHeight = EditorGUIUtility.singleLineHeight;

        if (property.isExpanded)
        {
            var keysProperty = property.FindPropertyRelative(KeysFieldName);
            var valuesProperty = property.FindPropertyRelative(ValuesFieldName);

            foreach (var entry in EnumerateEntries(keysProperty, valuesProperty))
            {
                var keyProperty = entry.keyProperty;
                var valueProperty = entry.valueProperty;
                var keyPropertyHeight = GetKeyPropertyHeight(keyProperty);
                var valuePropertyHeight = GetValuePropertyHeight(valueProperty);
                var lineHeight = GetKeyValueLinePropertyHeight(keyPropertyHeight, valuePropertyHeight);
                propertyHeight += lineHeight;
            }

            if (_mConflictIndex != -1)
            {
                propertyHeight += _mConflictLineHeight;
            }
        }

        return propertyHeight;
    }

    private static readonly Dictionary<SerializedPropertyType, PropertyInfo> ms_serializedPropertyValueAccessorsDict;

    static SerializableDictionaryPropertyDrawer()
    {
        var serializedPropertyValueAccessorsNameDict =
            new Dictionary<SerializedPropertyType, string>()
            {
                {SerializedPropertyType.Integer, "intValue"},
                {SerializedPropertyType.Boolean, "boolValue"},
                {SerializedPropertyType.Float, "floatValue"},
                {SerializedPropertyType.String, "stringValue"},
                {SerializedPropertyType.Color, "colorValue"},
                {SerializedPropertyType.ObjectReference, "objectReferenceValue"},
                {SerializedPropertyType.LayerMask, "intValue"},
                {SerializedPropertyType.Enum, "intValue"},
                {SerializedPropertyType.Vector2, "vector2Value"},
                {SerializedPropertyType.Vector3, "vector3Value"},
                {SerializedPropertyType.Vector4, "vector4Value"},
                {SerializedPropertyType.Rect, "rectValue"},
                {SerializedPropertyType.ArraySize, "intValue"},
                {SerializedPropertyType.Character, "intValue"},
                {SerializedPropertyType.AnimationCurve, "animationCurveValue"},
                {SerializedPropertyType.Bounds, "boundsValue"},
                {SerializedPropertyType.Quaternion, "quaternionValue"},
            };
        var serializedPropertyType = typeof(SerializedProperty);

        ms_serializedPropertyValueAccessorsDict = new Dictionary<SerializedPropertyType, PropertyInfo>();
        var flags = BindingFlags.Instance | BindingFlags.Public;

        foreach (var kvp in serializedPropertyValueAccessorsNameDict)
        {
            var propertyInfo = serializedPropertyType.GetProperty(kvp.Value, flags);
            ms_serializedPropertyValueAccessorsDict.Add(kvp.Key, propertyInfo);
        }
    }

    private static GUIContent IconContent(string name, string tooltip)
    {
        var builtinIcon = EditorGUIUtility.IconContent(name);
        return new GUIContent(builtinIcon.image, tooltip);
    }

    private static void DeleteArrayElementAtIndex(SerializedProperty arrayProperty, int index)
    {
        var property = arrayProperty.GetArrayElementAtIndex(index);
        // if(arrayProperty.arrayElementType.StartsWith("PPtr<$"))
        if (property.propertyType == SerializedPropertyType.ObjectReference)
        {
            property.objectReferenceValue = null;
        }

        arrayProperty.DeleteArrayElementAtIndex(index);
    }

    public static object GetPropertyValue(SerializedProperty p)
    {
        PropertyInfo propertyInfo;
        if (ms_serializedPropertyValueAccessorsDict.TryGetValue(p.propertyType, out propertyInfo))
        {
            return propertyInfo.GetValue(p, null);
        }
        return p.isArray ? GetPropertyValueArray(p) : GetPropertyValueGeneric(p);
    }

    private static void SetPropertyValue(SerializedProperty p, object v)
    {
        PropertyInfo propertyInfo;
        if (ms_serializedPropertyValueAccessorsDict.TryGetValue(p.propertyType, out propertyInfo))
        {
            propertyInfo.SetValue(p, v, null);
        }
        else
        {
            if (p.isArray)
                SetPropertyValueArray(p, v);
            else
                SetPropertyValueGeneric(p, v);
        }
    }

    private static object GetPropertyValueArray(SerializedProperty property)
    {
        var array = new object[property.arraySize];
        for (int i = 0; i < property.arraySize; i++)
        {
            SerializedProperty item = property.GetArrayElementAtIndex(i);
            array[i] = GetPropertyValue(item);
        }
        return array;
    }

    private static object GetPropertyValueGeneric(SerializedProperty property)
    {
        var dict = new Dictionary<string, object>();
        var iterator = property.Copy();
        if (iterator.Next(true))
        {
            var end = property.GetEndProperty();
            do
            {
                var name = iterator.name;
                var value = GetPropertyValue(iterator);
                dict.Add(name, value);
            } while (iterator.Next(false) && iterator.propertyPath != end.propertyPath);
        }
        return dict;
    }

    private static void SetPropertyValueArray(SerializedProperty property, object v)
    {
        var array = (object[]) v;
        property.arraySize = array.Length;
        for (var i = 0; i < property.arraySize; i++)
        {
            var item = property.GetArrayElementAtIndex(i);
            SetPropertyValue(item, array[i]);
        }
    }

    private static void SetPropertyValueGeneric(SerializedProperty property, object v)
    {
        var dict = (Dictionary<string, object>) v;
        var iterator = property.Copy();
        if (iterator.Next(true))
        {
            var end = property.GetEndProperty();
            do
            {
                string name = iterator.name;
                SetPropertyValue(iterator, dict[name]);
            } while (iterator.Next(false) && iterator.propertyPath != end.propertyPath);
        }
    }

    private struct EnumerationEntry
    {
        public readonly SerializedProperty keyProperty;
        public readonly SerializedProperty valueProperty;
        public readonly int index;

        public EnumerationEntry(SerializedProperty keyProperty, SerializedProperty valueProperty, int index)
        {
            this.keyProperty = keyProperty;
            this.valueProperty = valueProperty;
            this.index = index;
        }
    }

    private static IEnumerable<EnumerationEntry> EnumerateEntries(SerializedProperty keyArrayProperty,
        SerializedProperty valueArrayProperty, int startIndex = 0)
    {
        if (keyArrayProperty.arraySize > startIndex)
        {
            var index = startIndex;
            var keyProperty = keyArrayProperty.GetArrayElementAtIndex(startIndex);
            var valueProperty = valueArrayProperty.GetArrayElementAtIndex(startIndex);
            var endProperty = keyArrayProperty.GetEndProperty();

            do
            {
                yield return new EnumerationEntry(keyProperty, valueProperty, index);
                index++;
            } while (keyProperty.Next(false) && valueProperty.Next(false) &&
                     !SerializedProperty.EqualContents(keyProperty, endProperty));
        }
    }
}

public class SingleLineSerializableDictionaryPropertyDrawer : SerializableDictionaryPropertyDrawer
{
    protected override float DrawKeyValueLine(SerializedProperty keyProperty, SerializedProperty valueProperty,
        Rect linePosition, int index)
    {
        var labelWidth = EditorGUIUtility.labelWidth;

        var keyPropertyHeight = GetKeyPropertyHeight(keyProperty);
        var keyPosition = linePosition;
        keyPosition.height = keyPropertyHeight;
        keyPosition.xMax = labelWidth;
        EditorGUIUtility.labelWidth = labelWidth * keyPosition.width / linePosition.width;
        DrawKeyProperty(keyProperty, keyPosition, GUIContent.none);

        var valuePropertyHeight = GetValuePropertyHeight(valueProperty);
        var valuePosition = linePosition;
        valuePosition.height = valuePropertyHeight;
        valuePosition.xMin = labelWidth;
        EditorGUIUtility.labelWidth = labelWidth * valuePosition.width / linePosition.width;
        DrawValueProperty(valueProperty, valuePosition, GUIContent.none);

        EditorGUIUtility.labelWidth = labelWidth;

        return GetKeyValueLinePropertyHeight(keyPropertyHeight, valuePropertyHeight);
    }

    protected override float GetKeyValueLinePropertyHeight(float keyPropertyHeight, float valuePropertyHeight)
    {
        return Mathf.Max(keyPropertyHeight, valuePropertyHeight);
    }
}

public class DoubleLineSerializableDictionaryPropertyDrawer : SerializableDictionaryPropertyDrawer
{
    protected override float DrawKeyValueLine(SerializedProperty keyProperty, SerializedProperty valueProperty,
        Rect linePosition, int index)
    {
        var keyPropertyHeight = GetKeyPropertyHeight(keyProperty);
        var keyPosition = linePosition;
        keyPosition.height = keyPropertyHeight;
        DrawKeyProperty(keyProperty, keyPosition, new GUIContent("Key - " + index));

        var valuePropertyHeight = GetValuePropertyHeight(valueProperty);
        var valuePosition = linePosition;
        valuePosition.height = valuePropertyHeight;
        valuePosition.y += keyPropertyHeight;
        DrawValueProperty(valueProperty, valuePosition, new GUIContent("Value - " + index));

        return GetKeyValueLinePropertyHeight(keyPropertyHeight, valuePropertyHeight);
    }

    protected override float GetKeyValueLinePropertyHeight(float keyPropertyHeight, float valuePropertyHeight)
    {
        return keyPropertyHeight + valuePropertyHeight;
    }
}
#endif
