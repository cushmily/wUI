using System;
using System.Collections.Generic;
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

    [CreateAssetMenu(menuName = "wLib/UIStack/New Database")]
    public class WidgetDatabaseContainer : ScriptableObject
    {
        public string JsonData = "{}";
    }
}