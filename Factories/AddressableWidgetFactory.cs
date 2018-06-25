using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace wLib.UIStack
{
    [Serializable]
    public struct WidgetDatabase
    {
        public List<WidgetAddress> Addresses;

        public WidgetDatabase(List<WidgetAddress> addresses)
        {
            Addresses = addresses;
        }
    }

    [Serializable]
    public struct WidgetAddress
    {
        public string Name;
        public string Path;

        public WidgetAddress(string name, string path)
        {
            Name = name;
            Path = path;
        }
    }

    [CustomWidgetFactory(typeof(AddressableWidget))]
    public class AddressableWidgetFactory : IWidgetFactory<AddressableWidget>
    {
        private Dictionary<string, string> WidgetLookupDictionary = new Dictionary<string, string>();

        public AddressableWidgetFactory()
        {
            var database = Resources.Load<TextAsset>("UI Widget Database");
            var datas = JsonUtility.FromJson<WidgetDatabase>(database.text);
            var addresses = datas.Addresses;
            for (var i = 0; i < addresses.Count; i++)
            {
                var data = addresses[i];
                WidgetLookupDictionary.Add(data.Name, data.Path);
            }
        }

        public void CreateInstance(IUIManager manager, string widgetName, int assignedId, Action<BaseWidget> onCreated)
        {
            CreateInstance(manager, widgetName, assignedId, (AddressableWidget widget) => onCreated.Invoke(widget));
        }

        public void CreateInstance(IUIManager manager, string widgetName, int assignedId,
            Action<AddressableWidget> onCreated)
        {
            string widgetPath;
            if (!WidgetLookupDictionary.TryGetValue(widgetName, out widgetPath))
            {
                Debug.LogErrorFormat("Widget[{0}] not found.", widgetName);
                return;
            }

            var widget = UnityEngine.Object.Instantiate(Resources.Load<AddressableWidget>(widgetPath));
            widget.SetManagerInfo(assignedId, manager);

            if (onCreated != null) { onCreated.Invoke(widget); }
        }
    }
}