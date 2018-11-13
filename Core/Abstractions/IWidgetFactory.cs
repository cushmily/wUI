using System;

namespace wLib.UIStack
{
    public interface IWidgetFactory
    {
        void SetupFactory();
        
        void CreateInstance(IUIManager manager, string widgetName, int assignedId, Action<Widget> onCreated);
    }

    public interface IWidgetFactory<out TWidget> : IWidgetFactory where TWidget : Widget
    {
        void CreateInstance(IUIManager manager, string widgetName, int assignedId, Action<TWidget> onCreated);
    }
}