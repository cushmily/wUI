using System;
using System.Collections.Generic;
using UnityEngine;

namespace wLib.UIStack
{
    [Serializable]
    public class WidgetPairDict : SerializableDictionary<string, Widget>
    {
        public WidgetPairDict() { }
        
        public WidgetPairDict(IDictionary<string, Widget> widget) : base(widget) { }
    }

    [CreateAssetMenu(menuName = "wLib/UIStack/New Database")]
    public class WidgetDatabase : ScriptableObject
    {
        public WidgetPairDict Value = new WidgetPairDict();
    }
}