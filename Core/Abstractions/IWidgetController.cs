using System;

namespace wLib.UIStack
{
    public interface IWidgetController : IDisposable
    {
        void SetControllerInfo(Widget widget, IUIManager manager, UIMessage message);
        void Initialize();
        void OnDestroy();
    }
}