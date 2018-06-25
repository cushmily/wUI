using System;
using UnityEngine;

namespace wLib.UIStack
{
    [CustomWidgetFactory(typeof(BaseWidget))]
    public class DefaultWidgetFactory : IWidgetFactory<BaseWidget>
    {
        public void CreateInstance(IUIManager manager, string widgetPath, int assignedId,
            Action<BaseWidget> onCreated)
        {
            var widget = UnityEngine.Object.Instantiate(Resources.Load<BaseWidget>(widgetPath));
            widget.SetManagerInfo(assignedId, manager);

            if (onCreated != null) { onCreated.Invoke(widget); }
        }
    }
}