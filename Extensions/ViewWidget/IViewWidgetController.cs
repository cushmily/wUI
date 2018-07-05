namespace wLib.UIStack
{
    public interface IViewWidgetController
    {
        void SetWidget(IUIManager manager, object widget, int widgetId);

        void OnShow(UIMessage message);

        void OnHide();

        void OnUpdate();

        void OnResume();

        void OnFreeze();
    }

    public interface IViewWidgetController<out TView> : IViewWidgetController where TView : ViewWidget
    {
        TView Widget { get; }
    }

    public abstract class ViewWidgetController<TView> : IViewWidgetController<TView> where TView : ViewWidget
    {
        public IUIManager UiManager { get; private set; }

        public TView Widget { get; private set; }

        public int WidgetId { get; private set; }

        public virtual void SetWidget(IUIManager manager, object widget, int widgetId)
        {
            UiManager = manager;
            Widget = widget as TView;
            WidgetId = widgetId;
        }

        public virtual void OnShow(UIMessage message) { }

        public virtual void OnHide() { }
        
        public virtual void OnUpdate() { }

        public virtual void OnResume() { }

        public virtual void OnFreeze() { }
    }
}