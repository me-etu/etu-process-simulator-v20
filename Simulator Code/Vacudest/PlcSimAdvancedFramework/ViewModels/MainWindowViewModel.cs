using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using PlcSimAdvancedFramework.Commands;
using PlcSimAdvancedFramework.Models;
using PlcSimAdvancedFramework.Services;

namespace PlcSimAdvancedFramework.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly PlcSimRuntimeService runtimeService;
        private string instanceName;
        private string operatingState;
        private string projectName;
        private TagDefinition selectedTag;

        public MainWindowViewModel()
        {
            runtimeService = new PlcSimRuntimeService();
            runtimeService.StatusChanged += OnStatusChanged;
            runtimeService.OperatingStateChanged += state => OperatingState = state;

            InstanceName = "Evonik Framework SIM";
            OperatingState = "Disconnected";
            StatusEntries = new ObservableCollection<string>();
            Tags = new ObservableCollection<TagDefinition>();

            AttachCommand = new RelayCommand(_ => Execute(AttachToInstance));
            PowerOnCommand = new RelayCommand(_ => Execute(runtimeService.PowerOn), _ => runtimeService.IsConnected);
            PowerOffCommand = new RelayCommand(_ => Execute(runtimeService.PowerOff), _ => runtimeService.IsConnected);
            RunCommand = new RelayCommand(_ => Execute(runtimeService.Run), _ => runtimeService.IsConnected);
            StopCommand = new RelayCommand(_ => Execute(runtimeService.Stop), _ => runtimeService.IsConnected);
            RefreshTagsCommand = new RelayCommand(_ => Execute(runtimeService.RefreshTagList), _ => runtimeService.IsConnected);
            AddTagCommand = new RelayCommand(_ => AddTag());
            RemoveTagCommand = new RelayCommand(_ => RemoveSelectedTag(), _ => SelectedTag != null);
            ReadTagCommand = new RelayCommand(tag => ReadTag(tag as TagDefinition), _ => runtimeService.IsConnected);
            WriteTagCommand = new RelayCommand(tag => WriteTag(tag as TagDefinition), _ => runtimeService.IsConnected);
            LoadSampleProjectCommand = new RelayCommand(_ => LoadProject(GetSampleProjectPath()));
            SaveProjectCommand = new RelayCommand(_ => SaveProject(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs", "CustomProject.xml")));

            LoadProject(GetSampleProjectPath());
        }

        public ObservableCollection<string> StatusEntries { get; }
        public ObservableCollection<TagDefinition> Tags { get; }

        public string InstanceName
        {
            get { return instanceName; }
            set { SetProperty(ref instanceName, value); }
        }

        public string OperatingState
        {
            get { return operatingState; }
            set { SetProperty(ref operatingState, value); }
        }

        public string ProjectName
        {
            get { return projectName; }
            set { SetProperty(ref projectName, value); }
        }

        public TagDefinition SelectedTag
        {
            get { return selectedTag; }
            set
            {
                if (SetProperty(ref selectedTag, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public ICommand AttachCommand { get; }
        public ICommand PowerOnCommand { get; }
        public ICommand PowerOffCommand { get; }
        public ICommand RunCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand RefreshTagsCommand { get; }
        public ICommand AddTagCommand { get; }
        public ICommand RemoveTagCommand { get; }
        public ICommand ReadTagCommand { get; }
        public ICommand WriteTagCommand { get; }
        public ICommand LoadSampleProjectCommand { get; }
        public ICommand SaveProjectCommand { get; }

        private void AttachToInstance()
        {
            runtimeService.AttachOrCreate(InstanceName);
            OperatingState = runtimeService.CurrentState;
        }

        private void AddTag()
        {
            var tag = new TagDefinition
            {
                DisplayName = "New Tag",
                Address = "MyTag",
                DataType = TagDataType.Bool,
                ManualValue = "false",
                Description = "Edit address and type."
            };

            Tags.Add(tag);
            SelectedTag = tag;
        }

        private void RemoveSelectedTag()
        {
            if (SelectedTag == null)
            {
                return;
            }

            Tags.Remove(SelectedTag);
            SelectedTag = null;
        }

        private void ReadTag(TagDefinition tag)
        {
            if (tag == null)
            {
                return;
            }

            tag.LastReadValue = runtimeService.ReadTag(tag);
            OnStatusChanged($"Read {tag.Address} = {tag.LastReadValue}");
        }

        private void WriteTag(TagDefinition tag)
        {
            if (tag == null)
            {
                return;
            }

            runtimeService.WriteTag(tag);
            tag.LastReadValue = tag.ManualValue;
        }

        private void LoadProject(string path)
        {
            if (!File.Exists(path))
            {
                OnStatusChanged($"Project file not found: {path}");
                return;
            }

            var project = SimulationProject.Load(path);
            ProjectName = project.Name;
            Tags.Clear();
            foreach (var tag in project.Tags)
            {
                Tags.Add(tag);
            }

            OnStatusChanged($"Loaded project '{ProjectName}'.");
        }

        private void SaveProject(string path)
        {
            var project = new SimulationProject { Name = string.IsNullOrWhiteSpace(ProjectName) ? "Custom Project" : ProjectName };
            foreach (var tag in Tags)
            {
                project.Tags.Add(tag);
            }

            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            project.Save(path);
            OnStatusChanged($"Saved project to {path}");
        }

        private string GetSampleProjectPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs", "SampleProject.xml");
        }

        private void Execute(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                OnStatusChanged(ex.Message);
            }
        }

        private void OnStatusChanged(string message)
        {
            StatusEntries.Insert(0, $"{DateTime.Now:HH:mm:ss} {message}");
        }
    }
}
