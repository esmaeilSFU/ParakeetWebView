using System;
using System.Threading;
using System.Windows.Threading;
using Grasshopper.Kernel;

namespace GHUI
{
    public class HtmlUiComponent : GH_Component
    {
        public bool Initialized;

        private WebWindow _webWindow;
        private Thread _uiThread;
        private string _oldPath;

        public HtmlUiComponent()
            : base("Launch HTML UI", "HTML UI",
                "Launch a UI Window from a HTML file.",
                "UI", "Main")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("HTML Path", "path",
                "Where to look for the HTML interface.",
                GH_ParamAccess.item);

            pManager.AddBooleanParameter("Show Window", "show",
                "Toggle for showing/hiding the interface window.",
                GH_ParamAccess.item, false);

            pManager.AddTextParameter("Title", "title",
                "The title name for the UI window.",
                GH_ParamAccess.item, "UI");
            pManager.AddIntegerParameter("Width", "W",
                "Window width", GH_ParamAccess.item, 500);

            pManager.AddIntegerParameter("Height", "H",
                "Window height", GH_ParamAccess.item, 900);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Input Values", "vals",
                "Value of HTML Inputs", GH_ParamAccess.list);

            pManager.AddTextParameter("Input Ids", "ids",
                "Ids of HTML Inputs", GH_ParamAccess.list);

            pManager.AddTextParameter("Input Names", "names",
                "Names of HTML Inputs", GH_ParamAccess.list);

            pManager.AddTextParameter("Input Types", "types",
                "Types of HTML Inputs", GH_ParamAccess.list);

            pManager.AddGenericParameter("Web Window", "web",
                "Web Window Instance", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            string path = null;
            bool show = false;
            string title = null;
            int width = 500;
            int height = 900;

            if (!da.GetData(0, ref path)) return;
            da.GetData(1, ref show);
            da.GetData(2, ref title);
            da.GetData(3, ref width);
            da.GetData(4, ref height);

            // اگر show خاموش شد پنجره بسته شود
            if (!show)
            {
                CloseWindow();
                return;
            }

            if (Initialized && _webWindow != null)
            {
                if (_oldPath != path)
                {
                    _webWindow.Navigate(path);
                    _oldPath = path;
                }

                // update window size dynamically
                _webWindow.Dispatcher.Invoke(() =>
                {
                    _webWindow.Width = width;
                    _webWindow.Height = height;
                });

                // update window title dynamically
                _webWindow.Dispatcher.Invoke(() =>
                {
                    if (_webWindow.Title != title)
                        _webWindow.Title = title;
                });

                da.SetDataList(0, _webWindow.InputValues);
                da.SetDataList(1, _webWindow.InputIds);
                da.SetDataList(2, _webWindow.InputNames);
                da.SetDataList(3, _webWindow.InputTypes);
                da.SetData(4, _webWindow);
            }
            else
            {
                LaunchWindow(path, title, width, height);
                Initialized = true;
                _oldPath = path;
            }

            // refresh GH
            //OnPingDocument()?.ScheduleSolution(200, doc => ExpireSolution(false));
        }

        private void LaunchWindow(string path, string title , int width, int height)
        {
            if (_uiThread != null && _uiThread.IsAlive)
                return;

            _uiThread = new Thread(() =>
            {
                SynchronizationContext.SetSynchronizationContext(
                    new DispatcherSynchronizationContext(
                        Dispatcher.CurrentDispatcher));

                _webWindow = new WebWindow(path, this);

                _webWindow.Width = width;
                _webWindow.Height = height;

                _webWindow.Topmost = true;   


                _webWindow.Title = title;
                _webWindow.Closed += _webWindow_Closed;

                _webWindow.Show();
                _webWindow.Activate();

                Dispatcher.Run();
            });

            _uiThread.SetApartmentState(ApartmentState.STA);
            _uiThread.IsBackground = true;
            _uiThread.Start();
        }

        private void CloseWindow()
        {
            try
            {
                if (_webWindow != null)
                {
                    _webWindow.Dispatcher.Invoke(() =>
                    {
                        _webWindow.Close();
                    });

                    _webWindow = null;
                    _uiThread = null;
                    Initialized = false;
                }
            }
            catch { }
        }

        private void _webWindow_Closed(object sender, EventArgs e)
        {
            Initialized = false;
            _webWindow = null;
            _uiThread = null;

            Dispatcher.CurrentDispatcher.InvokeShutdown();
        }

        protected override System.Drawing.Bitmap Icon =>
            Properties.Resources.web_window;

        public override Guid ComponentGuid =>
            new Guid("1c7a71f6-4e49-4a7b-a67d-b7691dc381b4");
    }
}