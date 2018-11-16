using System.Linq;
using UnityEditor;
using UnityEngine;
using wLib.UIStack;

[CustomEditor(typeof(WidgetDatabase))]
public class WidgetDatabaseEditor : Editor
{
    private WidgetDatabase Target;

    private string _newWidgetPath;
    private Object _newWidget;

    private void OnEnable()
    {
        Target = target as WidgetDatabase;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        NewWidgetPanel();

        EditorGUILayout.Separator();

        ShowWidgets();
    }

    private void NewWidgetPanel()
    {
        EditorGUILayout.LabelField("New Widget:");
        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            var oriWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 45;

            using (new GUILayout.HorizontalScope())
            {
                _newWidgetPath =
                    EditorGUILayout.TextField(new GUIContent("Path"), _newWidgetPath, GUILayout.Width(120));
                _newWidget = EditorGUILayout.ObjectField(new GUIContent("Widget"), _newWidget, typeof(Widget), false);
                AddWidget();
            }

            EditorGUIUtility.labelWidth = oriWidth;
        }
    }

    private void ShowWidgets()
    {
        EditorGUILayout.LabelField($"Widgets[{Target.Value.Count}]:");
        foreach (var valuePair in Target.Value)
        {
            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(valuePair.Key);
                using (var checkScope = new EditorGUI.ChangeCheckScope())
                {
                    var newWidget = EditorGUILayout.ObjectField(valuePair.Value, typeof(Widget), false);
                    if (checkScope.changed)
                    {
                        Target.Value[valuePair.Key] = newWidget as Widget;
                        return;
                    }
                }

                if (RemoveButton(valuePair.Key)) { return; }
            }
        }
    }

    private void AddWidget()
    {
        var keyExists = string.IsNullOrEmpty(_newWidgetPath) || Target.Value.ContainsKey(_newWidgetPath);
        using (new EditorGUI.DisabledScope(keyExists))
        {
            if (!GUILayout.Button("Add", EditorStyles.toolbarButton, GUILayout.Width(45))) { return; }

            Target.Value.Add(_newWidgetPath, _newWidget as Widget);
            Target.Value = new WidgetPairDict(Target.Value.OrderBy(x => x.Key).ToDictionary(x => x.Key, v => v.Value));
            _newWidgetPath = "";
            _newWidget = null;

            SaveData();
        }
    }

    private bool RemoveButton(string key)
    {
        var ret = GUILayout.Button("-", EditorStyles.toolbarButton, GUILayout.Width(20));
        if (ret)
        {
            Target.Value.Remove(key);

            SaveData();
        }

        return ret;
    }

    private void SaveData()
    {
        serializedObject.ApplyModifiedProperties();
    }
}