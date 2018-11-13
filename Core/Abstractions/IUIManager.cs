using System;

namespace wLib.UIStack
{
    public interface IUIManager
    {
        void Push(string widgetName);

        void Push(string widgetName, Action<int> onCreated);

        void Push(string widgetName, UIMessage message);

        void Push(string widgetName, UIMessage message, Action<int> onCreated);
        
        void Push<TWidget>(string widgetName) where TWidget : Widget;

        void Push<TWidget>(string widgetName, Action<int> onCreated) where TWidget : Widget;

        void Push<TWidget>(string widgetName, UIMessage message) where TWidget : Widget;

        void Push<TWidget>(string widgetName, UIMessage message, Action<int> onCreated) where TWidget : Widget; 

        void Pop();

        void Pop(Action onDone);

        void ClearPopups();

        void ClearFixes();

        void ClearWindows();

        void ClearAll();

        void Close(int widgetId);

        void Close(int widgetId, Action onClosed);
    }
}