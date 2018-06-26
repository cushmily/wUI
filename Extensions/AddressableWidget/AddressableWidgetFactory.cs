using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace wLib.UIStack
{
    [CustomWidgetFactory(typeof(AddressableWidget))]
    public class AddressableWidgetFactory : IWidgetFactory<AddressableWidget>
    {
        private Dictionary<string, string> WidgetLookupDictionary = new Dictionary<string, string>();

        public AddressableWidgetFactory()
        {
            var databases = Resources.FindObjectsOfTypeAll<WidgetDatabaseContainer>();
            for (var i = 0; i < databases.Length; i++)
            {
                var database = databases[i];
                var datas = JsonUtility.FromJson<WidgetDatabase>(database.JsonData);
                var addresses = datas.Addresses;
                for (var j = 0; j < addresses.Count; j++)
                {
                    var data = addresses[j];
                    if (WidgetLookupDictionary.ContainsKey(data.Name))
                    {
                        Debug.LogErrorFormat("Widget called [{0}] has duplicated defined.", data.Name);
                    }

                    WidgetLookupDictionary[data.Name] = data.Path;
                }
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