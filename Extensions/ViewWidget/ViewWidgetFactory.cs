using System;
using System.Collections.Generic;
using System.Linq;

namespace wLib.UIStack
{
    [CustomWidgetFactory(typeof(ViewWidget))]
    public class ViewWidgetFactory : AddressableWidgetFactory, IWidgetFactory<ViewWidget>
    {
        private readonly Dictionary<Type, Type> _controllerRef = new Dictionary<Type, Type>();

        public ViewWidgetFactory()
        {
            var controllerTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
                .Where(x => !x.IsAbstract && !x.IsInterface && typeof(IWidgetController).IsAssignableFrom(x) &&
                            x != typeof(IWidgetController)).ToArray();

            foreach (var controllerType in controllerTypes)
            {
                var interfaces = controllerType.GetInterfaces();
                for (var i = 0; i < interfaces.Length; i++)
                {
                    var intf = interfaces[i];
                    if (!typeof(IWidgetController).IsAssignableFrom(intf) || intf == typeof(IWidgetController))
                    {
                        continue;
                    }

                    var args = intf.GetGenericArguments();
                    if (args.Length <= 0) { continue; }

                    var targetType = args[0];
                    _controllerRef.Add(targetType, controllerType);
                }
            }
        }

        public void CreateInstance(IUIManager manager, string widgetName, int assignedId,
            Action<ViewWidget> onCreated)
        {
            CreateInstance(manager, widgetName, assignedId, (AddressableWidget widget) =>
            {
                var viewWidget = (ViewWidget) widget;
                viewWidget.SetManagerInfo(assignedId, manager);
                var widgetType = viewWidget.GetType();

                if (_controllerRef.ContainsKey(widgetType))
                {
                    var controllerType = _controllerRef[widgetType];
                    var controller = Activator.CreateInstance(controllerType) as IWidgetController;
                    if (controller != null)
                    {
                        controller.SetWidget(viewWidget);
                        viewWidget.OnShowAction += controller.OnShow;
                        viewWidget.OnHideAction += controller.OnHide;
                        viewWidget.OnFreezeAction += controller.OnFreeze;
                        viewWidget.OnResumeAction += controller.OnResume;
                        viewWidget.OnDestroyAction += () =>
                        {
                            viewWidget.OnShowAction -= controller.OnShow;
                            viewWidget.OnHideAction -= controller.OnHide;
                            viewWidget.OnFreezeAction -= controller.OnFreeze;
                            viewWidget.OnResumeAction -= controller.OnResume;
                            controller = null;
                        };
                    }
                }

                if (onCreated != null) { onCreated.Invoke(viewWidget); }
            });
        }
    }
}