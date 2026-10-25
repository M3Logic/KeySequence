namespace Terminal.Gui.KeySequence;

internal sealed record CandidateMatch (CandidateMatchKind Kind, int Count, IReadOnlyDictionary<string, object?> Values)
{
    public KeySequenceBinding? Binding { get; init; }

    public static CandidateMatch NoMatch => new (CandidateMatchKind.NoMatch, 1, new Dictionary<string, object?> ());

    public static CandidateMatch Prefix (int count, IReadOnlyDictionary<string, object?> values) => new (CandidateMatchKind.Prefix, count, values);

    public static CandidateMatch Complete (int count, IReadOnlyDictionary<string, object?> values) => new (CandidateMatchKind.Complete, count, values);
}
