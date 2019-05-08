using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using wLib.Injection;

namespace wLib.UIStack
{
    public enum UILayer
    {
        UIHidden = -1,
        Background = 0,
        Window = 1,
        Fixed = 2,
        Popup = 3,
        Mask = 4
    }

    public class UIManager : MonoBehaviour, IUIManager
    {
        private readonly Stack<Widget> StackedWindows = new Stack<Widget>();
        private readonly List<int> WindowsInDisplay = new List<int>();
        private readonly List<int> Popups = new List<int>();
        private readonly List<int> Fixes = new List<int>();

        private readonly Dictionary<int, Widget> WidgetLookup = new Dictionary<int, Widget>();
        private readonly Dictionary<UILayer, GameObject> LayerLookup = new Dictionary<UILayer, GameObject>();
        private static readonly Dictionary<Type, IWidgetFactory> FactoryLookup = new Dictionary<Type, IWidgetFactory>();

        private readonly Dictionary<string, Stack<Widget>> PoolingWidgets =
            new Dictionary<string, Stack<Widget>>();

        private static IDependencyContainer _container;

        public static UIManager FromInstance(IDependencyContainer container, UIManager uiManagerPrefab)
        {
            if (_container != null)
            {
                Debug.LogError("UI Manager already initialized.");
                return null;
            }

            _container = container;
            CollectFactories();

            var instance = Instantiate(uiManagerPrefab);

            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                var layerLabel = layer.ToString();

                var child = instance.transform.Find(layerLabel);
                if (child == null)
                {
                    Debug.LogError($"Layer {layerLabel} can not be found in UI Manager.");
                    continue;
                }

                instance.LayerLookup.Add(layer, child.gameObject);
            }

            return instance;
        }

        public static UIManager BuildHierarchy(IDependencyContainer container, bool landscapeOrientation = true,
            Vector2? refResolution = null)
        {
            if (_container != null)
            {
                Debug.LogError("UI Manager already initialized.");
                return null;
            }

            _container = container;
            CollectFactories();

            var manager = new GameObject("UI Manager").AddComponent<UIManager>();

            var uiCam = new GameObject("UI Camera", typeof(Camera)).GetComponent<Camera>();
            uiCam.clearFlags = CameraClearFlags.Depth;
            uiCam.cullingMask = 1 << LayerMask.NameToLayer("UI");
            uiCam.orthographic = true;
            uiCam.depth = 10;
            uiCam.transform.SetParent(manager.transform);
            uiCam.transform.localPosition = Vector3.zero;

            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                var layerObj = new GameObject(layer.ToString());
                manager.LayerLookup.Add(layer, layerObj);

                var layerCanvas = layerObj.AddComponent<Canvas>();
                layerCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                layerCanvas.worldCamera = uiCam;
                layerCanvas.sortingOrder = (int) layer;

                var layerCanvasScaler = layerObj.AddComponent<CanvasScaler>();
                layerCanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                layerCanvasScaler.referenceResolution = refResolution ?? (landscapeOrientation
                                                            ? new Vector2(1920, 1080)
                                                            : new Vector2(1080, 1920));
                layerCanvasScaler.matchWidthOrHeight = landscapeOrientation ? 1 : 0;

                var layerRaycaster = layerObj.AddComponent<GraphicRaycaster>();
                layerRaycaster.name = layer.ToString();
                layerRaycaster.transform.SetParent(manager.transform);
                layerRaycaster.transform.localPosition = Vector3.zero;

                if (layer == UILayer.UIHidden)
                {
                    layerObj.layer = LayerMask.NameToLayer("UIHidden");
                    layerRaycaster.enabled = false;
                }
                else { layerObj.layer = LayerMask.NameToLayer("UI"); }
            }

            return manager;
        }

        #region Widget Manager

        private int _componentId;

        #region Push

        public void Push(string widgetName)
        {
            Push<Widget>(widgetName, UIMessage.Empty, null);
        }

        public void Push(string widgetName, Action<int> onCreated)
        {
            Push<Widget>(widgetName, UIMessage.Empty, onCreated);
        }

        public void Push(string widgetName, UIMessage message)
        {
            Push<Widget>(widgetName, message, null);
        }

        public void Push(string widgetName, UIMessage message, Action<int> onCreated)
        {
            Push<Widget>(widgetName, message, onCreated);
        }

        public void Push<TWidget>() where TWidget : Widget
        {
            Push<TWidget>(null, UIMessage.Empty, null);
        }

        public void Push<TWidget>(Action<int> onCreated) where TWidget : Widget
        {
            Push<TWidget>(null, UIMessage.Empty, onCreated);
        }

        public void Push<TWidget>(UIMessage message) where TWidget : Widget
        {
            Push<TWidget>(null, message, null);
        }

        public void Push<TWidget>(UIMessage message, Action<int> onCreated) where TWidget : Widget
        {
            Push<TWidget>(null, UIMessage.Empty, null);
        }

        public void Push<TWidget>(string widgetName) where TWidget : Widget
        {
            Push<TWidget>(widgetName, UIMessage.Empty, null);
        }

        public void Push<TWidget>(string widgetName, Action<int> onCreated) where TWidget : Widget
        {
            Push<TWidget>(widgetName, UIMessage.Empty, onCreated);
        }

        public void Push<TWidget>(string widgetName, UIMessage message) where TWidget : Widget
        {
            Push<TWidget>(widgetName, message, null);
        }

        public void Push<TWidget>(string widgetName, UIMessage message, Action<int> onCreated)
            where TWidget : Widget
        {
            var id = GetId();
            GetInstance<TWidget>(widgetName, id, message, instance =>
            {
                var parent = LayerLookup[instance.Layer];
                instance.transform.SetParent(parent.transform, false);
//                instance.transform.SetAsFirstSibling();

                if (instance.Layer == UILayer.Popup) { Popups.Add(instance.Id); }

                if (instance.Layer == UILayer.Fixed) { Fixes.Add(instance.Id); }

                if (StackedWindows.Count > 0)
                {
                    var prevWidget = StackedWindows.Peek();
                    RunCoroutine(prevWidget.OnFreeze(), () =>
                    {
                        prevWidget.TriggerOnFreezeEvent();

                        // Window will overlay previous windows.
                        if (instance.Layer == UILayer.Window && WindowsInDisplay.Contains(prevWidget.Id))
                        {
                            WindowsInDisplay.Remove(prevWidget.Id);
                        }
                    });
                    RunCoroutine(instance.OnShow(), () =>
                    {
                        instance.TriggerOnShowEvent();
                        onCreated?.Invoke(id);

                        StackedWindows.Push(instance);
                        WidgetLookup.Add(id, instance);
                        WindowsInDisplay.Add(id);
                    });
                }
                else
                {
                    RunCoroutine(instance.OnShow(), () =>
                    {
                        instance.TriggerOnShowEvent();
                        onCreated?.Invoke(id);

                        StackedWindows.Push(instance);
                        WidgetLookup.Add(id, instance);
                        WindowsInDisplay.Add(id);
                    });
                }
            });
        }

        #endregion

        #region Pop

        public void Pop(bool recycle = false)
        {
            Pop(null, recycle);
        }

        public void Pop(Action onDone, bool recycle = false)
        {
            if (StackedWindows.Count < 0)
            {
                Debug.LogWarning("Nothing to pop.");
                return;
            }

            var current = StackedWindows.Pop();

            if (StackedWindows.Count > 0)
            {
                RunCoroutine(current.OnHide(), () =>
                {
                    current.TriggerOnHideEvent();

                    if (recycle) { MoveToHidden(current); }
                    else
                    {
                        WidgetLookup.Remove(current.Id);
                        Destroy(current.gameObject);
                    }

                    current.Controller?.OnDestroy();
                    onDone?.Invoke();

                    // resume previous window
                    var resumeWindow = StackedWindows.Peek();
                    RunCoroutine(resumeWindow.OnResume(), () => { resumeWindow.TriggerOnResumeEvent(); });
                });
            }
            else
            {
                RunCoroutine(current.OnHide(), () =>
                {
                    current.TriggerOnHideEvent();

                    if (recycle) { MoveToHidden(current); }
                    else
                    {
                        WidgetLookup.Remove(current.Id);
                        Destroy(current.gameObject);
                    }

                    current.Controller?.OnDestroy();
                    onDone?.Invoke();
                });
            }
        }

        #endregion

        #region Clear

        public void ClearPopups()
        {
            foreach (var popup in Popups) { Close(popup); }

            Popups.Clear();
        }

        public void ClearFixes()
        {
            foreach (var fix in Fixes) { Close(fix); }

            Fixes.Clear();
        }

        public void ClearWindows()
        {
            while (StackedWindows.Count > 0)
            {
                var window = StackedWindows.Pop();
                Close(window.Id);
            }
        }

        public void ClearAll()
        {
            ClearPopups();
            ClearFixes();
            ClearWindows();
        }

        public void Close(int widgetId, bool recycle = false)
        {
            Close(widgetId, null, recycle);
        }

        public void Close(int widgetId, Action onClosed, bool recycle = false)
        {
            var targetWidget = Get(widgetId);
            if (targetWidget.Layer != UILayer.Window || !WindowsInDisplay.Contains(widgetId))
            {
                RunCoroutine(targetWidget.OnHide(), () =>
                {
                    if (recycle) { MoveToHidden(targetWidget); }
                    else
                    {
                        WidgetLookup.Remove(targetWidget.Id);
                        Destroy(targetWidget.gameObject);
                    }

                    targetWidget.Controller?.OnDestroy();
                    onClosed?.Invoke();

                    if (WindowsInDisplay.Contains(widgetId)) { WindowsInDisplay.Remove(widgetId); }
                });
            }
        }

        #endregion

        #endregion

        #region Helpers

        public static void RegisterFactory(Type type, IWidgetFactory factory)
        {
            if (FactoryLookup.ContainsKey(type))
            {
                Debug.LogErrorFormat("Factory already registered for type: {0}.", type);
            }

            FactoryLookup[type] = factory;
        }

        #endregion

        #region Internal Funtions

        private static void CollectFactories()
        {
            if (FactoryLookup.Count > 0) { return; }

            var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
                .Where(x => typeof(IWidgetFactory).IsAssignableFrom(x));

            foreach (var factoryType in types)
            {
                if (!factoryType.IsAbstract && !factoryType.IsInterface)
                {
                    var atts = factoryType.GetCustomAttributes(typeof(CustomWidgetFactoryAttribute), true);
                    if (atts.Length <= 0) { continue; }

                    if (!(atts[0] is CustomWidgetFactoryAttribute att)) { continue; }

//                        Debug.Log("Collect " + att.WidgetType);
                    if (!(Activator.CreateInstance(factoryType) is IWidgetFactory factoryInstance)) { continue; }

                    _container.Inject(factoryInstance);
                    factoryInstance.SetupFactory();
                    RegisterFactory(att.WidgetType, factoryInstance);
                }
            }
        }

        private void GetInstance<T>(string widgetPath, int assignedId, UIMessage message, Action<T> onCreated)
            where T : Widget
        {
            var resolveType = typeof(T);
            if (PoolingWidgets.ContainsKey(widgetPath))
            {
                var pool = PoolingWidgets[widgetPath];
                if (pool.Count > 0)
                {
                    var instance = pool.Pop();

                    if (instance.Controller != null)
                    {
                        try
                        {
                            instance.Controller?.SetControllerInfo(instance, this, message);
                            instance.Controller?.Initialize();
                        }
                        catch (Exception ex) { Debug.LogException(ex); }
                    }

                    onCreated.Invoke(instance as T);
                    return;
                }
            }

            var useSpecifiedFactory = false;
            if (!FactoryLookup.TryGetValue(resolveType, out var factory))
            {
                while (factory == null && resolveType.BaseType != null)
                {
                    resolveType = resolveType.BaseType;
                    FactoryLookup.TryGetValue(resolveType, out factory);
                }

                if (factory == null)
                {
                    Debug.LogError($"Widget factory not found for type: {typeof(T)}, no fallback.");
                    return;
                }
            }
            else { useSpecifiedFactory = true; }

            // fallback
            if (useSpecifiedFactory)
            {
                var specifiedFactory = factory as IWidgetFactory<T>;
                specifiedFactory?.CreateInstance(this, widgetPath, assignedId, message, onCreated);
            }
            else
            {
                factory.CreateInstance(this, widgetPath, assignedId, message,
                    widgetCreated =>
                    {
                        var genericWidget = widgetCreated as T;
                        if (genericWidget == null)
                        {
                            Debug.LogWarningFormat("Can not convert [{0}] to type: {1}", widgetCreated, typeof(T));
                        }

                        onCreated.Invoke(genericWidget);
                    });
            }
        }

        private void RunCoroutine(IEnumerator target, Action onDone)
        {
            StartCoroutine(MonitorCoroutine(target, onDone));
        }

        private IEnumerator MonitorCoroutine(IEnumerator target, Action onDone)
        {
            yield return target;
            onDone?.Invoke();
        }

        private int GetId()
        {
            return _componentId++;
        }

        public Widget Get(int id)
        {
            if (!WidgetLookup.TryGetValue(id, out var targetComp))
            {
                Debug.LogWarningFormat("Can't load widget of id: {0}.", id);
            }

            return targetComp;
        }

        public TUiComponent Get<TUiComponent>(int id) where TUiComponent : Widget
        {
            if (!WidgetLookup.TryGetValue(id, out var targetComp))
            {
                Debug.LogWarningFormat("Can't load widget of id: {0}.", id);
            }
            else
            {
                var resultComponent = targetComp as TUiComponent;
                if (resultComponent != null) { return resultComponent; }
            }

            return null;
        }

        private void MoveToHidden(Widget toHide)
        {
            var hiddenLayer = LayerLookup[UILayer.UIHidden];
            toHide.transform.SetParent(hiddenLayer.transform);

            var type = toHide.GetType();
            if (PoolingWidgets.ContainsKey(toHide.Path)) { PoolingWidgets[toHide.Path].Push(toHide); }
            else
            {
                var newStack = new Stack<Widget>();
                newStack.Push(toHide);
                PoolingWidgets.Add(toHide.Path, newStack);
            }
        }

        #endregion
    }
}