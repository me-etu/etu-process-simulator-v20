# PlcSimAdvancedFramework

Reusable PLCSIM Advanced simulator framework for tag-based testing.

## What it does
- Attaches to or creates a PLCSIM Advanced instance.
- Powers the instance on/off and switches RUN/STOP.
- Refreshes the symbolic tag list.
- Reads and writes generic Bool, Int16, and Float tags.
- Loads and saves tag mappings from XML.

## Main files
- Views/MainWindow.xaml: generic simulator UI.
- ViewModels/MainWindowViewModel.cs: app workflow and commands.
- Services/PlcSimRuntimeService.cs: PLCSIM Advanced API wrapper.
- Configs/SampleProject.xml: starter tag mapping.

## Suggested next steps
1. Build the solution in Visual Studio.
2. Open the `PlcSimAdvancedFramework` project as the startup project.
3. Start PLCSIM Advanced.
4. Click `Attach`, `Power On`, `Refresh Tags`, then `Run`.
5. Read/write the sample tags, then replace them with your real project mappings.
