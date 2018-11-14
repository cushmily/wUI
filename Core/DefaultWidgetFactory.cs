using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using wLib.Injection;
using Object = UnityEngine.Object;

namespace wLib.UIStack
{
    [CustomWidgetFactory(typeof(Widget))]
    public class DefaultWidgetFactory : IWidgetFactory<Widget>
    {
        [Inject]
        private DiContainer _container;

        private readonly List<WidgetDatabase> _databases;

        private readonly Dictionary<string, Widget> _caches = new Dictionary<string, Widget>();

        private readonly Dictionary<Type, Type> _controllerRef = new Dictionary<Type, Type>();

        public DefaultWidgetFactory()
        {
            _databases = Resources.LoadAll<WidgetDatabase>("").ToList();
            Debug.Log($"Found {_databases.Count} databases.");
        }

        public void SetupFactory()
        {
            CollectControllers();
        }

        private void CollectControllers()
        {
            var controllerTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => !x.IsAbstract && !x.IsInterface
                                          && typeof(IWidgetController).IsAssignableFrom(x)
                                          && x != typeof(IWidgetController))
                .ToArray();

            foreach (var controllerType in controllerTypes)
            {
                // only depth 1 allowed.
                var baseType = controllerType.BaseType;
                if (baseType != null)
                {
                    var args = baseType.GetGenericArguments();
                    if (args.Length <= 0) { continue; }

                    var targetType = args[0];
                    _controllerRef.Add(targetType, controllerType);
                }
            }
        }

        public void CreateInstance(IUIManager manager, string widgetPath, int assignedId,
            Action<Widget> onCreated)
        {
            if (!_caches.ContainsKey(widgetPath))
            {
                for (var i = 0; i < _databases.Count; i++)
                {
                    var database = _databases[i];
                    if (!database.Value.ContainsKey(widgetPath)) { continue; }

                    var result = database.Value[widgetPath];
                    _caches.Add(widgetPath, result);
                }
            }


            if (!_caches.ContainsKey(widgetPath))

            {
                Debug.LogError($"Can't found widget@{widgetPath}");
                return;
            }

            var ret = _caches[widgetPath];
            ret.SetManagerInfo(assignedId, manager);
            var instance = Object.Instantiate(ret);
            var instanceType = instance.GetType();
            if (_controllerRef.ContainsKey(instanceType))
            {
                var controllerType = _controllerRef[instanceType];
                var controllerInstance =
                    Activator.CreateInstance(controllerType, new object[] {instance}) as IWidgetController;
                _container.Inject(controllerInstance);
                controllerInstance?.Initialise();
            }

            onCreated?.Invoke(instance);
        }
    }
}