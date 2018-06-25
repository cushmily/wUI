using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using wLib.UIStack;

namespace wLib
{
    public class AddressableWidgetEditorWindow : EditorWindow
    {
        private TextAsset _databaseTextAsset;

        private WidgetDatabase _database;

        private Dictionary<string, BaseWidget> _widgets = new Dictionary<string, BaseWidget>();

        private string _newAddresableName;

        private int _selectedGroup;

        [MenuItem("wLib/UI Widget Manager/Manage Widgets")]
        private static void ShowWindow()
        {
            var window = GetWindow<AddressableWidgetEditorWindow>();
            window.name = "Widget Database";
            window.ShowUtility();
        }

        private void OnEnable()
        {
            _databaseTextAsset = Resources.Load<TextAsset>("UI Widget Database");

            if (_databaseTextAsset != null) { LoadFromAll(); }
        }

        private void LoadFromAll()
        {
            _database = JsonUtility.FromJson<WidgetDatabase>(_databaseTextAsset.text);
            _widgets.Clear();

            for (var i = 0; i < _database.Addresses.Count; i++)
            {
                var address = _database.Addresses[i];
                var widget = Resources.Load<BaseWidget>(address.Path);

                _widgets.Add(address.Name, widget);
            }
        }

        private void SaveToJson()
        {
            var database = new WidgetDatabase(new List<WidgetAddress>());
            foreach (var widget in _widgets)
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

            var outputPath = AssetDatabase.GetAssetPath(_databaseTextAsset);

            File.WriteAllText(outputPath, JsonUtility.ToJson(database));
            AssetDatabase.Refresh();

            Debug.Log("Save to: " + outputPath);
        }


        private void OnGUI()
        {
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton)) { LoadFromAll(); }

                if (GUILayout.Button("Save", EditorStyles.toolbarButton)) { SaveToJson(); }
            }

            EditorGUILayout.LabelField("Addressable Widget List:");

            using (new GUILayout.HorizontalScope())
            {
                _newAddresableName = EditorGUILayout.TextField("New Widget Name", _newAddresableName);
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_newAddresableName) ||
                                                   _widgets.ContainsKey(_newAddresableName)))
                {
                    if (GUILayout.Button("Insert", EditorStyles.toolbarButton, GUILayout.Width(75)))
                    {
                        _widgets.Add(_newAddresableName, null);
                        _newAddresableName = string.Empty;
                    }
                }
            }

            var removedWidgets = new List<string>();

            EditorGUI.indentLevel++;

            var names = _widgets.Select(x => x.Key).ToArray();
            for (var i = 0; i < names.Length; i++)
            {
                var widgetName = names[i];
                using (new GUILayout.HorizontalScope())
                {
                    _widgets[widgetName] = EditorGUILayout
                        .ObjectField(widgetName, _widgets[widgetName], typeof(BaseWidget), false) as BaseWidget;

                    if (GUILayout.Button("REMOVE", EditorStyles.toolbarButton, GUILayout.Width(75)))
                    {
                        removedWidgets.Add(widgetName);
                    }
                }
            }

            for (var i = 0; i < removedWidgets.Count; i++) { _widgets.Remove(removedWidgets[i]); }

            EditorGUI.indentLevel--;
        }
    }
}