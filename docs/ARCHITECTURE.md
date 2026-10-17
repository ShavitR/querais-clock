# Architecture

Querais is a Windows-only WPF app targeting .NET 10, using native windows and `Viewport3D` without a webview or runtime service.

| File | Responsibility |
| --- | --- |
| `MainWindow.xaml.cs` | Shell, controls, popovers, shortcuts, time tools and tray |
| `SculptureView.cs` | Meshes, materials, lighting, interaction, hands, live displays, flaps and segments |
| `ClockSurface.cs` | Flat vector faces |
| `Preferences.cs` | LocalAppData storage and legacy Meridian import |
| `TimeText.cs` | Duration input and formatting |
| `Verification.cs` | In-process checks and visual-tree captures |

The clock follows local system time. Timer deadlines use UTC; stopwatch timing is monotonic. Alarms follow local wall time and need a running process.

A dedicated UI timer drives visible 3D animation. Hidden/minimized windows stop it. Display textures update when their time or settings change. Meshes are frozen and scenes are reused.

`assets/collection` is the runtime contract. The app never contacts Blender or MCP.
