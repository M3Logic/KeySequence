namespace Terminal.Gui.KeySequence.Tests;

public class KeySequenceBindingsTests
{
    [Theory]
    [InlineData (false)]
    [InlineData (true)]
    public void Modifier_Only_Events_Preserve_Pending_Sequences (bool persistent)
    {
        KeySequenceBindings bindings = new ()
            { Mode = persistent ? KeySequenceMode.Persistent : KeySequenceMode.Leader };
        int invocations = 0;
        List<KeySequenceResult> results = [];
        bindings.StateChanged += (_, args) => results.Add (args.Result);
        if (persistent)
        {
            bindings.AddMode ("d d", _ => ++invocations > 0);
        }
        else
        {
            bindings.Add ("; d d", _ => ++invocations > 0);
        }

        using View target = new ();
        Assert.Equal (KeySequenceResult.NotLeader, bindings.ProcessKey (target, Key.Empty.WithShift));
        Assert.Empty (results);
        bindings.ProcessKey (target, persistent ? Key.Esc : new Key (';'));
        Assert.Equal (KeySequenceResult.Pending, bindings.ProcessKey (target, 'd'));
        int notificationCount = results.Count;
        Key [] modifiers =
        [
            Key.Empty.WithShift,
            Key.Empty.WithCtrl,
            Key.Empty.WithAlt,
            new Key () { ModifierKey = ModifierKey.LeftShift },
            new Key () { ModifierKey = ModifierKey.RightShift },
            new Key () { ModifierKey = ModifierKey.LeftCtrl }
        ];

        foreach (Key modifier in modifiers)
        {
            Assert.Equal (KeySequenceResult.Pending, bindings.ProcessKey (target, modifier));
        }

        Assert.Equal (notificationCount, results.Count);
        Assert.True (bindings.IsCapturing);
        Assert.Equal (KeySequenceResult.Matched, bindings.ProcessKey (target, 'd'));
        Assert.Equal (1, invocations);
    }

    [Theory]
    [InlineData ("q", "Ctrl+Q")]
    [InlineData ("q", "Alt+Q")]
    [InlineData ("q", "Q")]
    [InlineData ("g", "G")]
    [InlineData ("<Tab>", "Shift+Tab")]
    public void Printable_Matching_Preserves_Shortcut_Modifiers_And_Case (string pattern, string input)
    {
        KeySequenceBindings bindings = new () { Mode = KeySequenceMode.Persistent };
        bindings.AddMode (pattern, _ => throw new InvalidOperationException ("Unexpected match"));
        bindings.EnterCommandMode ();
        using View target = new ();
        Assert.True (Key.TryParse (input, out Key key));

        Assert.Equal (KeySequenceResult.Rejected, bindings.ProcessKey (target, key));
    }

    [Fact]
    public void ProcessKey_Matches_Count_Motion ()
    {
        KeySequenceBindings bindings = new ();
        int moveCount = 0;

        bindings.Add ("; m <count> k", context =>
        {
            moveCount = context.Count;
            return true;
        });

        View target = new ();

        Assert.Equal (KeySequenceResult.Started, bindings.ProcessKey (target, ';'));
        Assert.Equal (KeySequenceResult.Pending, bindings.ProcessKey (target, 'm'));
        Assert.Equal (KeySequenceResult.Pending, bindings.ProcessKey (target, '4'));
        Assert.Equal (KeySequenceResult.Matched, bindings.ProcessKey (target, 'k'));
        Assert.Equal (4, moveCount);
        Assert.False (bindings.IsCapturing);
    }

    [Fact]
    public void ProcessKey_Matches_Operator_Count_Motion ()
    {
        KeySequenceBindings bindings = new ();
        int deleteCount = 0;
        Key? operatorKey = null;
        Key? motionKey = null;

        bindings.Add ("; d <count> d", context =>
        {
            deleteCount = context.Count;
            operatorKey = context.OperatorKey;
            motionKey = context.MotionKey;
            return true;
        });

        View target = new ();

        bindings.ProcessKey (target, ';');
        bindings.ProcessKey (target, 'd');
        bindings.ProcessKey (target, '2');

        Assert.Equal (KeySequenceResult.Matched, bindings.ProcessKey (target, 'd'));
        Assert.Equal (2, deleteCount);
        Assert.Equal (Key.D, operatorKey);
        Assert.Equal (Key.D, motionKey);
    }

    [Fact]
    public void ProcessKey_Uses_Default_Count_When_Count_Is_Omitted ()
    {
        KeySequenceBindings bindings = new ();
        int moveCount = 0;

        bindings.Add ("; m <count> k", context =>
        {
            moveCount = context.Count;
            return true;
        });

        View target = new ();

        bindings.ProcessKey (target, ';');
        bindings.ProcessKey (target, 'm');

        Assert.Equal (KeySequenceResult.Matched, bindings.ProcessKey (target, 'k'));
        Assert.Equal (1, moveCount);
    }

    [Fact]
    public void ProcessKey_Cancels_With_Escape ()
    {
        KeySequenceBindings bindings = new ();
        bindings.Add ("; m <count> k", _ => true);

        View target = new ();

        bindings.ProcessKey (target, ';');

        Assert.Equal (KeySequenceResult.Canceled, bindings.ProcessKey (target, Key.Esc));
        Assert.False (bindings.IsCapturing);
    }

    [Fact]
    public void Add_Rejects_Duplicate_Pattern ()
    {
        KeySequenceBindings bindings = new ();
        bindings.Add ("; m <count> k", _ => true);

        Assert.Throws<InvalidOperationException> (() => bindings.Add ("; m <count> k", _ => true));
    }

    [Fact]
    public void PersistentMode_Enters_Matches_And_Stays_In_Command_Mode ()
    {
        KeySequenceBindings bindings = new ()
        {
            Mode = KeySequenceMode.Persistent,
            EnterModeKey = Key.Esc,
            ExitModeKey = 'i'
        };
        int moveCount = 0;

        bindings.AddMode ("<count> k", context =>
        {
            moveCount = context.Count;
            Assert.True (context.IsCommandMode);
            Assert.Null (context.LeaderKey);
            return true;
        });

        View target = new ();

        Assert.Equal (KeySequenceResult.ModeEntered, bindings.ProcessKey (target, Key.Esc));
        Assert.True (bindings.IsCommandMode);
        Assert.Equal (KeySequenceResult.Pending, bindings.ProcessKey (target, '4'));
        Assert.Equal (KeySequenceResult.Matched, bindings.ProcessKey (target, 'k'));
        Assert.Equal (4, moveCount);
        Assert.True (bindings.IsCommandMode);
    }

    [Fact]
    public void PersistentMode_Exits_With_ExitModeKey ()
    {
        KeySequenceBindings bindings = new ()
        {
            Mode = KeySequenceMode.Persistent,
            EnterModeKey = Key.Esc,
            ExitModeKey = 'i'
        };

        View target = new ();

        bindings.ProcessKey (target, Key.Esc);

        Assert.Equal (KeySequenceResult.ModeExited, bindings.ProcessKey (target, 'i'));
        Assert.False (bindings.IsCommandMode);
    }

    [Fact]
    public void PersistentMode_Rejects_Invalid_Sequence_And_Stays_In_Command_Mode ()
    {
        KeySequenceBindings bindings = new ()
        {
            Mode = KeySequenceMode.Persistent,
            EnterModeKey = Key.Esc,
            ExitModeKey = 'i'
        };
        bindings.AddMode ("k", _ => true);

        View target = new ();

        bindings.ProcessKey (target, Key.Esc);

        Assert.Equal (KeySequenceResult.Rejected, bindings.ProcessKey (target, 'x'));
        Assert.True (bindings.IsCommandMode);
    }
}
