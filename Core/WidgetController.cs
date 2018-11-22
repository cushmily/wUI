namespace wLib.UIStack
{
    public abstract class WidgetController<TWidget> : IWidgetController where TWidget : Widget
    {
        protected TWidget View { get; private set; }

        protected IUIManager Manager { get; private set; }

        protected UIMessage Message { get; private set; }

        public virtual void SetControllerInfo(Widget widget, IUIManager manager, UIMessage message)
        {
            View = widget as TWidget;
            Manager = manager;
            Message = message;

            if (View != null) { View.OnDestroyEvent += OnViewDestroyed; }
        }

        private void OnViewDestroyed(Widget obj)
        {
            if (View != null) { View.OnDestroyEvent -= OnViewDestroyed; }

            Dispose();
        }

        public void Dispose()
        {
            OnDestroy();
        }

        public abstract void Initialize();

        public abstract void OnDestroy();
    }
}