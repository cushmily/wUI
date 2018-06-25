using System;

namespace wLib.UIStack
{
    public interface IWidgetFactory
    {
        void CreateInstance(IUIManager manager, string widgetName, int assignedId, Action<BaseWidget> onCreated);
    }

    public interface IWidgetFactory<out TWidget> : IWidgetFactory where TWidget : BaseWidget
    {
        void CreateInstance(IUIManager manager, string widgetName, int assignedId, Action<TWidget> onCreated);
    }
}