using System;

namespace wLib.UIStack
{
    public interface IUIManager
    {
        void Push<TWidget>(string widgetName) where TWidget : BaseWidget;
        
        void Push<TWidget>(string widgetName, Action<int> onCreated) where TWidget : BaseWidget;
        
        void Push<TWidget>(string widgetName, UIMessage message) where TWidget : BaseWidget;

        void Push<TWidget>(string widgetName, UIMessage message, Action<int> onCreated) where TWidget : BaseWidget;

        void Pop();
        
        void Pop(Action onPoped);

        void Close(int widgetId);
        
        void Close(int widgetId, Action onClosed);
    }
}