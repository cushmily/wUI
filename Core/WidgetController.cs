namespace wLib.UIStack
{
    public abstract class WidgetController<TWidget> : IWidgetController where TWidget : Widget
    {
        protected readonly TWidget View;

        protected WidgetController(TWidget widget)
        {
            View = widget;

            View.OnDestroyEvent += OnViewDestroyed;
        }

        private void OnViewDestroyed(Widget obj)
        {
            View.OnDestroyEvent -= OnViewDestroyed;
            Dispose();
        }

        public void Dispose()
        {
            OnDestroy();
        }

        public abstract void Initialise();

        public abstract void OnDestroy();
    }
}