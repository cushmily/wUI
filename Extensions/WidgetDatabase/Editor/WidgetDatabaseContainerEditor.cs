using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using wLib.UIStack;

namespace wLib
{
    [CustomEditor(typeof(WidgetDatabaseContainer))]
    public class WidgetDatabaseContainerEditor : Editor
    {
        private WidgetDatabaseContainer _container;

        private string _newAddresableName;

        private void OnEnable()
        {
            _container = target as WidgetDatabaseContainer;
            if (_container != null) { LoadFromJson(); }
        }

        public override void OnInspectorGUI()
        {
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton)) { LoadFromJson(); }

                if (GUILayout.Button("Save", EditorStyles.toolbarButton)) { SaveToJson(); }
            }

            EditorGUILayout.LabelField("Addressable Widget List:");

            EditorGUI.indentLevel++;
            EditorGUI.indentLevel--;

            using (new GUILayout.HorizontalScope())
            {
                _newAddresableName = EditorGUILayout.TextField("New Widget Name", _newAddresableName);
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_newAddresableName) ||
                                                   Widgets.ContainsKey(_newAddresableName)))
                {
                    if (GUILayout.Button("Insert", EditorStyles.toolbarButton, GUILayout.Width(75)))
                    {
                        Widgets.Add(_newAddresableName, null);
                        _newAddresableName = string.Empty;
                    }
                }
            }

            var removedWidgets = new List<string>();

            var widgets = Widgets;

            EditorGUI.indentLevel++;

            var names = widgets.Select(x => x.Key).ToArray();
            for (var j = 0; j < names.Length; j++)
            {
                var widgetName = names[j];
                using (new GUILayout.HorizontalScope())
                {
                    widgets[widgetName] = EditorGUILayout
                        .ObjectField(widgetName, widgets[widgetName], typeof(BaseWidget), false) as BaseWidget;

                    if (GUILayout.Button("REMOVE", EditorStyles.toolbarButton, GUILayout.Width(75)))
                    {
                        removedWidgets.Add(widgetName);
                    }
                }
            }

            for (var k = 0; k < removedWidgets.Count; k++) { widgets.Remove(removedWidgets[k]); }

            EditorGUI.indentLevel--;
        }

        public Dictionary<string, BaseWidget> Widgets = new Dictionary<string, BaseWidget>();

        private void LoadFromJson()
        {
            var database = JsonUtility.FromJson<WidgetDatabase>(_container.JsonData);

            for (var i = 0; i < database.Addresses.Count; i++)
            {
                var address = database.Addresses[i];
                var widgetObj = Resources.Load<BaseWidget>(address.Path);
                Widgets[address.Name] = widgetObj;
            }
        }

        private void SaveToJson()
        {
            var database = new WidgetDatabase(new List<WidgetAddress>());
            foreach (var widget in Widgets)
            {
                var path = widget.Value ? AssetDatabase.GetAssetPath(widget.Value) : string.Empty;
                if (!string.IsNullOrEmpty(path))
                {
                    if (Path.HasExtension(path))
                    {
                        var ext = Path.GetExtension(path);
                        path = path.Remove(path.IndexOf(ext, StringComparison.CurrentCulture), ext.Length);
                    }

                    var index = path.IndexOf("Resources/", StringComparison.CurrentCulture);
                    if (index < 0)
                    {
                        Debug.LogErrorFormat("Widget must be placed under Resources folders. [{0}]", path);
                        return;
                    }

                    path = path.Substring(index + 10);
                }

                database.Addresses.Add(new WidgetAddress(widget.Key, path));
            }

            _container.JsonData = JsonUtility.ToJson(database);
            EditorUtility.SetDirty(_container);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Save to: " + target.name);
        }
    }
}