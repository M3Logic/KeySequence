using Terminal.Gui.App;
using Moq;
using Terminal.Gui.Drivers;
using Terminal.Gui.KeySequence.Demo;
using TgEditor = Terminal.Gui.Editor.Editor;

namespace Terminal.Gui.KeySequence.Tests;

public class DemoKeySequenceTests
{
    private readonly IApplication _app;

    public DemoKeySequenceTests ()
    {
        Mock<IApplication> app = new () { DefaultValue = DefaultValue.Mock };
        app.SetupGet (instance => instance.Clipboard).Returns (new FakeClipboard ());
        _app = app.Object;
    }

    [Fact]
    public void Quit_Requires_Colon_Then_Q_In_Command_Mode ()
    {
        using TgEditor editor = new () { App = _app, Text = "alpha\nbeta\ngamma\ndelta\nepsilon", CaretOffset = 0 };
        KeySequencesDemo demo = new ();
        int quitRequests = 0;
        using IDisposable registration = demo.RegisterKeySequences (editor, () => quitRequests++);

        Send (editor, Key.Esc);
        Send (editor, ':');
        Assert.Equal (0, quitRequests);
        Assert.True (editor.GetKeySequences ().IsCapturing);

        Send (editor, 'q');
        Assert.Equal (1, quitRequests);
        Assert.Equal ("alpha\nbeta\ngamma\ndelta\nepsilon", editor.Text);
    }

    [Theory]
    [InlineData (0)]
    [InlineData (1)]
    [InlineData (2)]
    public void Quit_Accepts_Shift_Events_And_Shifted_Colon (int encoding)
    {
        using TgEditor editor = new () { App = _app, Text = "alpha", CaretOffset = 0 };
        KeySequencesDemo demo = new ();
        int quitRequests = 0;
        List<KeySequenceResult> results = [];
        using IDisposable registration = demo.RegisterKeySequences (editor, () => quitRequests++);
        editor.GetKeySequences ().StateChanged += (_, args) => results.Add (args.Result);
        Key colon = encoding switch
        {
            0 => new Key (':').WithShift,
            1 => new Key (new Key (';').WithShift) { ShiftedKeyCode = (KeyCode)':' },
            _ => new Key (new Key (';').WithShift) { AssociatedText = ":" }
        };

        Send (editor, Key.Esc);
        Send (editor, new Key (KeyCode.ShiftMask) { ModifierKey = ModifierKey.LeftShift });
        Send (editor, new Key (KeyCode.ShiftMask) { ModifierKey = ModifierKey.LeftShift });
        Send (editor, colon);
        Assert.Equal (0, quitRequests);
        Send (editor, new Key (KeyCode.ShiftMask) { ModifierKey = ModifierKey.LeftShift });
        Send (editor, 'q');

        Assert.Equal (1, quitRequests);
        Assert.Equal ("alpha", editor.Text);
        Assert.Equal ([KeySequenceResult.ModeEntered, KeySequenceResult.Pending, KeySequenceResult.Matched], results);
    }

    [Theory]
    [InlineData ("h", 8, 7)]
    [InlineData ("l", 8, 9)]
    [InlineData ("j", 8, 13)]
    [InlineData ("k", 8, 2)]
    [InlineData ("4j", 0, 23)]
    [InlineData ("2k", 11, 0)]
    [InlineData ("w", 8, 11)]
    [InlineData ("b", 8, 6)]
    [InlineData ("2w", 0, 11)]
    [InlineData ("0", 8, 6)]
    [InlineData ("$", 8, 10)]
    [InlineData ("gg", 8, 0)]
    [InlineData ("G", 8, 30)]
    public void Motions_Move_The_Editor_Caret (string sequence, int start, int expected)
    {
        using TgEditor editor = new () { App = _app, Text = "alpha\nbeta\ngamma\ndelta\nepsilon", CaretOffset = start };
        KeySequencesDemo demo = new ();
        using IDisposable registration = demo.RegisterKeySequences (editor, () => Assert.Fail ("Unexpected quit"));

        Send (editor, Key.Esc);
        SendText (editor, sequence);

        Assert.Equal (expected, editor.CaretOffset);
        Assert.Equal ("alpha\nbeta\ngamma\ndelta\nepsilon", editor.Text);
        Assert.True (editor.GetKeySequences ().IsCommandMode);
    }

    [Theory]
    [InlineData ("dd", "beta\ngamma\ndelta\nepsilon")]
    [InlineData ("3dd", "delta\nepsilon")]
    [InlineData ("x", "lpha\nbeta\ngamma\ndelta\nepsilon")]
    [InlineData ("2x", "pha\nbeta\ngamma\ndelta\nepsilon")]
    [InlineData ("llX", "apha\nbeta\ngamma\ndelta\nepsilon")]
    [InlineData ("ll2X", "pha\nbeta\ngamma\ndelta\nepsilon")]
    public void Editing_Changes_The_Buffer (string sequence, string expected)
    {
        using TgEditor editor = new () { App = _app, Text = "alpha\nbeta\ngamma\ndelta\nepsilon", CaretOffset = 0 };
        KeySequencesDemo demo = new ();
        using IDisposable registration = demo.RegisterKeySequences (editor, () => Assert.Fail ("Unexpected quit"));

        Send (editor, Key.Esc);
        SendText (editor, sequence);

        Assert.Equal (expected, editor.Text);
        Assert.True (editor.GetKeySequences ().IsCommandMode);
    }

    [Theory]
    [InlineData ("vl", 8, 8, 9)]
    [InlineData ("v2h", 8, 6, 8)]
    [InlineData ("vj", 8, 8, 13)]
    [InlineData ("vk", 8, 2, 8)]
    [InlineData ("vw", 8, 8, 11)]
    [InlineData ("vb", 8, 6, 8)]
    [InlineData ("v0", 8, 6, 8)]
    [InlineData ("v$", 8, 8, 10)]
    [InlineData ("vgg", 8, 0, 8)]
    [InlineData ("vG", 8, 8, 30)]
    public void Selection_Motions_Select_Text (string sequence, int start, int expectedStart, int expectedEnd)
    {
        using TgEditor editor = new () { App = _app, Text = "alpha\nbeta\ngamma\ndelta\nepsilon", CaretOffset = start };
        KeySequencesDemo demo = new ();
        using IDisposable registration = demo.RegisterKeySequences (editor, () => Assert.Fail ("Unexpected quit"));

        Send (editor, Key.Esc);
        SendText (editor, sequence);

        Assert.True (editor.HasSelection);
        Assert.Equal (expectedStart, editor.SelectionStart);
        Assert.Equal (expectedEnd, editor.SelectionEnd);
    }

    [Theory]
    [InlineData ("yy", "alpha\n")]
    [InlineData ("2yy", "alpha\nbeta\n")]
    public void Yank_Then_Paste_Copies_The_Requested_Lines (string sequence, string copied)
    {
        const string original = "alpha\nbeta\ngamma";
        using TgEditor editor = new () { App = _app, Text = original, CaretOffset = 2 };
        KeySequencesDemo demo = new ();
        using IDisposable registration = demo.RegisterKeySequences (editor, () => Assert.Fail ("Unexpected quit"));

        Send (editor, Key.Esc);
        SendText (editor, sequence);

        Assert.Equal (original, editor.Text);
        Assert.Equal (copied, _app.Clipboard!.GetClipboardData ());

        SendText (editor, "ggp");
        Assert.Equal (copied + original, editor.Text);
    }

    [Theory]
    [InlineData ("vy", "alpha\nbeta")]
    [InlineData ("vc", "pha\nbeta")]
    public void Copy_And_Cut_Use_The_Selection (string sequence, string expected)
    {
        using TgEditor editor = new () { App = _app, Text = "alpha\nbeta", CaretOffset = 0 };
        KeySequencesDemo demo = new ();
        using IDisposable registration = demo.RegisterKeySequences (editor, () => Assert.Fail ("Unexpected quit"));

        Send (editor, Key.Esc);
        SendText (editor, "v2l");
        Assert.Equal ("al", editor.SelectedText);
        SendText (editor, sequence);

        Assert.Equal ("al", _app.Clipboard!.GetClipboardData ());
        Assert.Equal (expected, editor.Text);
    }

    [Theory]
    [InlineData (false, false, 0, 6)]
    [InlineData (true, false, 12, 6)]
    [InlineData (false, true, 0, 6)]
    [InlineData (true, true, 12, 6)]
    public void Page_Motions_Move_And_Extend_By_The_Viewport_Height (bool backwards, bool select, int start,
        int expected)
    {
        using TgEditor editor = new ()
        {
            App = _app,
            Text = "a\nb\nc\nd\ne\nf\ng\nh\ni\nj",
            Frame = new (0, 0, 20, 3),
            CaretOffset = start
        };
        KeySequencesDemo demo = new ();
        using IDisposable registration = demo.RegisterKeySequences (editor, () => Assert.Fail ("Unexpected quit"));

        Send (editor, Key.Esc);
        if (select)
        {
            SendText (editor, "v");
        }

        Send (editor, backwards ? Key.B.WithCtrl : Key.F.WithCtrl);

        Assert.Equal (expected, editor.CaretOffset);
        Assert.Equal (select, editor.HasSelection);
        if (select)
        {
            Assert.Equal (Math.Min (start, expected), editor.SelectionStart);
            Assert.Equal (Math.Max (start, expected), editor.SelectionEnd);
        }
    }

    [Fact]
    public void Undo_And_Redo_Restore_The_Edited_Buffer ()
    {
        using TgEditor editor = new () { App = _app, Text = "alpha\nbeta", CaretOffset = 0 };
        KeySequencesDemo demo = new ();
        using IDisposable registration = demo.RegisterKeySequences (editor, () => Assert.Fail ("Unexpected quit"));

        Send (editor, Key.Esc);
        SendText (editor, "x");
        Assert.Equal ("lpha\nbeta", editor.Text);
        SendText (editor, "u");
        Assert.Equal ("alpha\nbeta", editor.Text);
        Send (editor, Key.R.WithCtrl);
        Assert.Equal ("lpha\nbeta", editor.Text);
    }

    [Fact]
    public void Exit_Mode_Resumes_Normal_Text_Input ()
    {
        using TgEditor editor = new () { App = _app, Text = "alpha", CaretOffset = 0 };
        KeySequencesDemo demo = new ();
        using IDisposable registration = demo.RegisterKeySequences (editor, () => Assert.Fail ("Unexpected quit"));

        Send (editor, Key.Esc);
        SendText (editor, "i");
        Assert.False (editor.GetKeySequences ().IsCommandMode);
        SendText (editor, ":qdd");
        Assert.Equal (":qddalpha", editor.Text);
    }

    private static void SendText (TgEditor editor, string text)
    {
        foreach (char character in text)
        {
            Send (editor, character);
        }
    }

    private static void Send (TgEditor editor, Key key)
    {
        Assert.True (editor.NewKeyDownEvent (key));
    }
}
