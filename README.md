# KeySequence

Leader-key sequences and persistent command mode for Terminal.Gui v2 applications.
KeySequence attaches through public keyboard events, so applications can add it
alongside Terminal.Gui without patching the toolkit.

- Leader sequences such as `; m 4 k` and `; d 2 d`
- Persistent command mode: `Esc` to enter, `i` to return to normal input
- Numeric counts, printable-character and key wildcards
- View-scoped or application-scoped bindings and state notifications
- Disposable registrations

Project/package: **KeySequence**. Root namespace: **Terminal.Gui.KeySequence**.
Requires .NET 10 and Terminal.Gui 2.4.3 or a later 2.x release.

## Install from a local package

This repository starts at `0.1.0-alpha.1`; it has not yet been published to NuGet.org.
To build an installable package, clone the repository and run:

```powershell
dotnet pack src/KeySequence -c Release -o artifacts
```

To install it into an application, run these commands in the application's directory:

```powershell
dotnet nuget add source D:\repos\KeySequence\artifacts --name KeySequence-local
dotnet add package KeySequence --version 0.1.0-alpha.1
```

NuGet restores Terminal.Gui as a dependency. An existing compatible Terminal.Gui
reference can stay in the application.

## Leader sequences

To move up with `; m 4 k`, attach a binding to a view:

```csharp
using Terminal.Gui.Input;
using Terminal.Gui.KeySequence;
using Terminal.Gui.Views;

TextView editor = new ();
using IDisposable registration = editor.UseKeySequences (bindings =>
{
    bindings.Add ("; m <count> k", context =>
    {
        for (int i = 0; i < context.Count; i++)
        {
            context.Target.InvokeCommand (Command.Up);
        }

        return true;
    });
});

// Add editor to the UI and run the application while registration is alive.
```

To intercept character input before a view's normal bindings, pass
`KeySequenceInterceptionMode.Preemptive` to `UseKeySequences` where supported by
the view's key routing. The default, `AfterUnhandled`, starts with unhandled keys.

## Persistent command mode

To use `Esc` to enter command mode, remove any conflicting application binding
and register command-mode patterns:

```csharp
app.Keyboard.KeyBindings.Remove (Key.Esc);
using IDisposable registration = editor.UseKeySequences (
    bindings =>
    {
        bindings.Mode = KeySequenceMode.Persistent;
        bindings.EnterModeKey = Key.Esc;
        bindings.ExitModeKey = 'i';
        bindings.AddMode ("<count> k", context =>
        {
            for (int i = 0; i < context.Count; i++)
            {
                context.Target.InvokeCommand (Command.Up);
            }

            return true;
        });
    },
    KeySequenceInterceptionMode.Preemptive);
```

## Build, test, and demo

```powershell
dotnet restore
dotnet build -c Release --no-restore
dotnet test --project tests/KeySequence.Tests -c Release --no-build
dotnet run --project examples/KeySequence.Demo
```

The standalone editor demo supports motions, counts, line operations, undo/redo,
and selection sequences. Press `Esc` to enter command mode, `i` to exit, and
`: q` in command mode to quit. The demo alone depends on Terminal.Gui.Editor;
the KeySequence library depends only on Terminal.Gui.

## Documentation

- [Usage guide](docs/keysequences.md)
- [Specification](docs/specification.md)
- [Terminal.Gui](https://github.com/tui-cs/Terminal.Gui)

## License

[MIT](LICENSE).
