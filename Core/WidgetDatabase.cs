using System;
using UnityEngine;

namespace wLib.UIStack
{
    [Serializable]
    public class WidgetPairDict : SerializableDictionary<string, Widget> { }

    [CreateAssetMenu(menuName = "wLib/UIStack/New Database")]
    public class WidgetDatabase : ScriptableObject
    {
        public WidgetPairDict Value;
    }
}