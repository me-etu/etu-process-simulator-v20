using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CoSimulationPlcSimAdv.Models;
using CoSimulationPlcSimAdv.ViewModels;
using Siemens.Simatic.Simulation.Runtime;
using Microsoft.VisualBasic;
using System.Globalization;


namespace CoSimulationPlcSimAdv.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>    

    public partial class MainWindow : Window 
    {
        private readonly Dictionary<string, StackPanel> unitPanels = new Dictionary<string, StackPanel>(StringComparer.OrdinalIgnoreCase);

        public CoSimulationPlcSimAdv.App App { get; set; }
        public Simulation Simulation { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            BuildConfiguredUnitPanels();
            ShowUnit("All");
            this.DataContext = new ViewModels.MainWindowViewModel();
            this.Loaded += OnLoaded;
            App = Application.Current as CoSimulationPlcSimAdv.App;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            App.Initialize();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ViewModels.MainWindowViewModel Application = this.DataContext as ViewModels.MainWindowViewModel;

            if (Application?.virtualController != null)
            {
                Application.virtualController.Dispose();
            }
        }

        private void TimeFactor_Setpoint_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            try
            {
                App.Simulation.TimeFact = float.Parse(TimeFactor_Setpoint.Text, CultureInfo.InvariantCulture.NumberFormat);
            }
            catch {}
        }

        private void UnitSidebarButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            ShowUnit(button?.Tag as string ?? "All");
        }

        private void ShowUnit(string unitKey)
        {
            var showAll = unitKey.Equals("All", StringComparison.OrdinalIgnoreCase);
            var showCommissioningDb = unitKey.Equals("CommissioningDb", StringComparison.OrdinalIgnoreCase);
            BaseUnitPanel.Visibility = showAll || unitKey.Equals("Base", StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
            CommissioningDbPanel.Visibility = showCommissioningDb
                ? Visibility.Visible
                : Visibility.Collapsed;

            foreach (var unitPanel in unitPanels)
            {
                unitPanel.Value.Visibility = !showCommissioningDb && (showAll || unitPanel.Key.Equals(unitKey, StringComparison.OrdinalIgnoreCase))
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void CommissioningBool_Checked(object sender, RoutedEventArgs e)
        {
            WriteCommissioningBool(sender as CheckBox, true);
        }

        private void CommissioningBool_Unchecked(object sender, RoutedEventArgs e)
        {
            WriteCommissioningBool(sender as CheckBox, false);
        }

        private void CommissioningReal_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            CommitCommissioningReal(sender as TextBox);
        }

        private void CommissioningReal_LostFocus(object sender, RoutedEventArgs e)
        {
            CommitCommissioningReal(sender as TextBox);
        }

        private void WriteCommissioningBool(CheckBox checkBox, bool value)
        {
            var variable = checkBox?.Tag as CommissioningDbVariable;
            var viewModel = DataContext as MainWindowViewModel;
            viewModel?.WriteCommissioningBool(variable, value);
        }

        private void CommitCommissioningReal(TextBox textBox)
        {
            var variable = textBox?.Tag as CommissioningDbVariable;
            var viewModel = DataContext as MainWindowViewModel;
            viewModel?.WriteCommissioningReal(variable, textBox?.Text);
        }

        private void BuildConfiguredUnitPanels()
        {
            foreach (var unit in DeviceUiConfigLoader.LoadUnitConfigs())
            {
                var unitPanel = new StackPanel
                {
                    Margin = new Thickness(0, 0, 0, 18)
                };

                unitPanel.Children.Add(new TextBlock
                {
                    Text = unit.UnitName,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 16,
                    Margin = new Thickness(0, 0, 0, 8)
                });

                AddAnalogSection(unitPanel, unit);
                AddDigitalSection(unitPanel, unit);
                AddValveSection(unitPanel, unit);

                if (unitPanel.Children.Count <= 1)
                {
                    continue;
                }

                ConfiguredUnitPanels.Children.Add(unitPanel);
                unitPanels[unit.UnitName] = unitPanel;

                var unitButton = new Button
                {
                    Content = unit.UnitName,
                    Tag = unit.UnitName,
                    Height = 28,
                    Margin = new Thickness(0, 0, 0, 6)
                };
                unitButton.Click += UnitSidebarButton_Click;
                UnitSidebarPanel.Children.Add(unitButton);
            }
        }

        private void AddAnalogSection(StackPanel unitPanel, DeviceUiUnitConfig unit)
        {
            var analogs = unit.Config.analogInputs ?? new List<DeviceUiAnalogInput>();
            if (analogs.Count == 0)
            {
                return;
            }

            unitPanel.Children.Add(CreateSectionTitle("Analog Inputs"));
            var wrapPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 18) };

            foreach (var analog in analogs)
            {
                var controls = analog.uiControls ?? new DeviceUiAnalogControls();
                var card = new StackPanel { Width = 132, Margin = new Thickness(0, 0, 8, 8) };
                card.Children.Add(CreateToggleButton(controls.actualValueButton ?? analog.uiId + "_ActValueButton", "Error"));
                card.Children.Add(CreateToggleButton(controls.setpointButton ?? analog.uiId + "_SetpointButton", "Sim " + analog.displayName));
                card.Children.Add(CreateTextBox(controls.setpointEdit ?? analog.uiId + "_SetpointEdit"));
                wrapPanel.Children.Add(card);
            }

            unitPanel.Children.Add(wrapPanel);
        }

        private void AddDigitalSection(StackPanel unitPanel, DeviceUiUnitConfig unit)
        {
            var digitals = unit.Config.digitalInputs ?? new List<DeviceUiDigitalInput>();
            if (digitals.Count == 0)
            {
                return;
            }

            unitPanel.Children.Add(CreateSectionTitle("Digital Inputs"));
            var wrapPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 18) };

            foreach (var digital in digitals)
            {
                var controls = digital.uiControls ?? new DeviceUiDigitalControls();
                wrapPanel.Children.Add(CreateToggleButton(
                    controls.actualValueButton ?? digital.uiId + "_ActValueButton",
                    digital.displayName,
                    132,
                    new Thickness(0, 0, 8, 8)));
            }

            unitPanel.Children.Add(wrapPanel);
        }

        private void AddValveSection(StackPanel unitPanel, DeviceUiUnitConfig unit)
        {
            var valves = (unit.Config.valves ?? new List<DeviceUiValve>())
                .Where(valve => !DeviceUiConfigLoader.ShouldSkipValve(unit, valve))
                .ToList();

            if (valves.Count == 0)
            {
                return;
            }

            unitPanel.Children.Add(CreateSectionTitle("Valve Feedback"));
            var wrapPanel = new WrapPanel();

            foreach (var valve in valves)
            {
                var controls = valve.uiControls ?? new DeviceUiValveControls();
                var card = new StackPanel { Width = 196, Margin = new Thickness(0, 0, 8, 8) };
                card.Children.Add(CreateToggleButton(controls.simulateFeedbackButton ?? valve.uiId + "_SimFbButton", "Sim " + valve.displayName));

                var forcePanel = new StackPanel { Orientation = Orientation.Horizontal };
                forcePanel.Children.Add(CreateToggleButton(controls.forceOpenButton ?? valve.uiId + "_SimFbOpenButton", "OPEN", 98));
                forcePanel.Children.Add(CreateToggleButton(controls.forceCloseButton ?? valve.uiId + "_SimFbCloseButton", "CLOSE", 98));
                card.Children.Add(forcePanel);

                var qualityPanel = new StackPanel { Orientation = Orientation.Horizontal };
                qualityPanel.Children.Add(CreateToggleButton(controls.openQualityErrorButton ?? valve.uiId + "_QB_ErrorFbOpenButton", "Open QB", 98));
                qualityPanel.Children.Add(CreateToggleButton(controls.closeQualityErrorButton ?? valve.uiId + "_QB_ErrorFbCloseButton", "Close QB", 98));
                card.Children.Add(qualityPanel);

                card.Children.Add(CreateToggleButton(controls.manualControlButton ?? valve.uiId + "_MVCtrlButton", "Manual Ctrl " + valve.displayName));
                wrapPanel.Children.Add(card);
            }

            unitPanel.Children.Add(wrapPanel);
        }

        private TextBlock CreateSectionTitle(string text)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 6)
            };
        }

        private ToggleButton CreateToggleButton(string name, string content, double width = double.NaN, Thickness? margin = null)
        {
            var button = new ToggleButton
            {
                Content = content,
                Height = 24,
                Width = width,
                Margin = margin ?? new Thickness(0)
            };
            RegisterNamedElement(button, name);
            return button;
        }

        private TextBox CreateTextBox(string name)
        {
            var textBox = new TextBox
            {
                Height = 22,
                TextWrapping = TextWrapping.Wrap
            };
            RegisterNamedElement(textBox, name);
            return textBox;
        }

        private void RegisterNamedElement(FrameworkElement element, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            element.Name = name;
            if (RootGrid.FindName(name) == null)
            {
                RootGrid.RegisterName(name, element);
            }
        }
    }
}
