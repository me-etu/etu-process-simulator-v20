using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Siemens.Simatic.Simulation.Runtime;
using CoSimulationPlcSimAdv.Models;

namespace CoSimulationPlcSimAdv
{

    public delegate void LoopHandler(IInstance instance, bool init = false);

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public void Initialize()
        {
            if (Simulation == null)
            {
                Simulation = new Simulation();
            }
        }

        public event LoopHandler Loop;

        public void DoLoop(IInstance instance, bool init = false)
        {
            Instance = instance;

            var handlers = Loop;
            if (handlers == null)
            {
                return;
            }

            foreach (LoopHandler handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(instance, init);
                }
                catch (Exception ex)
                {
                    LogStatus($"Loop handler failed ({handler.Method.DeclaringType?.Name}.{handler.Method.Name}): {ex.Message}");
                }
            }
        }

        public void RequestLoopInitialization()
        {
        }

        public void StopRefreshLoop()
        {
        }

        public void LogStatus(string message)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (MainWindow?.DataContext is ViewModels.MainWindowViewModel viewModel)
                {
                    viewModel.WriteStatusEntry(message);
                }
            }));
        }

        public Simulation Simulation;
        public IInstance Instance = null;
    }
}
