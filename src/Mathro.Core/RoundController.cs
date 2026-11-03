using System.Data;
using System.Text;

namespace Mathro.Core;

public enum RoundPhase
{
    Selecting,
    Won,
    Lost,
}

public enum PlayStatus
{
    Err,
    Scored,
    RoundWon,
    RoundLost,
}

public record RoundSettings(
    int HandSize = 11,
    int PlaysPerRound = 5,
    int DiscardsPerRound = 4,
    int MaxCardsPerPlay = 7,
    int TotalMultiplier = 1
);

public readonly record struct ScoreBreakdown(double Base, double Multiplier)
{
    public long Total =>
        Math.Max(0, (long)Math.Round(Base * Multiplier, MidpointRounding.AwayFromZero));
}

public record PlayResult(PlayStatus Status, ScoreBreakdown Breakdown, IReadOnlyList<Card> Drawn)
{
    public bool Ok => Status != PlayStatus.Err;

    public static readonly PlayResult Err = new(PlayStatus.Err, default, []);
}

public record DiscardResult(bool Ok, List<Card> Drawn)
{
    public static readonly DiscardResult Err = new(false, []);
}

public class RoundController
{
    private readonly Deck _deck;
    private readonly List<Card> _hand = [];

    public RoundSettings Settings { get; }
    public int CurrentRound { get; }
    public long TargetScore { get; }

    public long AccumulatedScore { get; private set; }
    public int PlaysRemaining { get; private set; }
    public int DiscardsRemaining { get; private set; }
    public int Multiplier => Settings.TotalMultiplier;
    public RoundPhase Phase { get; private set; } = RoundPhase.Selecting;

    public int DeckRemaining => _deck.Remaining;
    public int DeckTotal => _deck.Total;

    public IReadOnlyList<Card> Hand
    {
        get { return _hand; }
    }

    public RoundController(Deck deck, RoundSettings settings, int roundNumber)
    {
        _deck = deck;
        Settings = settings;
        CurrentRound = roundNumber;

        TargetScore = (long)Math.Round(500 * Math.Pow(1.5, roundNumber - 1));
        PlaysRemaining = settings.PlaysPerRound;
        DiscardsRemaining = settings.DiscardsPerRound;

        _deck.Reset();
        RefillHand();
    }

    private ScoreBreakdown EvaluateCards(IReadOnlyList<Card> cards)
    {
        return new ScoreBreakdown(1, 1);
    }

    private ScoreBreakdown EvaluateMathString(IReadOnlyList<Card> cards, double multiplier)
    {
        StringBuilder equation = new();
        Card? prevCard = null;

        foreach (var card in cards)
        {
            if (prevCard?.Category == card.Category)
            {
                return new ScoreBreakdown(0, 0);
            }
            else
            {
                equation.Append(card.Value);
                prevCard = card;
            }
        }

        try
        {
            DataTable dt = new();
            var result = dt.Compute(equation.ToString(), "");
            var value = Convert.ToDouble(result);
            return new ScoreBreakdown(value, Multiplier);
        }
        catch
        {
            return new ScoreBreakdown(0, 0);
        }
    }

    public PlayResult PlayHand(IReadOnlyList<Card> cards)
    {
        if (!CanAct(cards, PlaysRemaining))
        {
            return PlayResult.Err;
        }

        var score = EvaluateMathString(cards, 1.0);

        PlaysRemaining--;
        AccumulatedScore += score.Total;
        RemoveFromHand(cards);

        if (HasMetTarget())
        {
            Phase = RoundPhase.Won;
        }
        else if (PlaysRemaining == 0 || (_hand.Count == 0 && _deck.Remaining == 0))
        {
            Phase = RoundPhase.Lost;
        }

        IReadOnlyList<Card> drawn = [];
        if (Phase == RoundPhase.Selecting)
        {
            drawn = RefillHand();
        }

        var status = Phase switch
        {
            RoundPhase.Won => PlayStatus.RoundWon,
            RoundPhase.Lost => PlayStatus.RoundLost,
            _ => PlayStatus.Scored,
        };

        return new PlayResult(status, score, drawn);
    }

    public bool HasMetTarget()
    {
        return AccumulatedScore >= TargetScore;
    }

    public DiscardResult Discard(IReadOnlyList<Card> cards)
    {
        if (!CanAct(cards, DiscardsRemaining))
        {
            return DiscardResult.Err;
        }

        DiscardsRemaining--;
        RemoveFromHand(cards);
        var drawn = RefillHand();

        if (_hand.Count == 0)
        {
            Phase = RoundPhase.Lost;
        }

        return new DiscardResult(true, drawn);
    }

    private void RemoveFromHand(IReadOnlyList<Card> cards)
    {
        foreach (var card in cards)
        {
            _hand.Remove(card);
        }
    }

    private bool CanAct(IReadOnlyList<Card> cards, int actionsLeft)
    {
        return Phase == RoundPhase.Selecting
            && actionsLeft > 0
            && cards.Count > 0
            && cards.Count <= Settings.MaxCardsPerPlay
            && cards.Distinct().Count() == cards.Count
            && cards.All(_hand.Contains);
    }

    private List<Card> RefillHand()
    {
        var drawn = _deck.Draw(Settings.HandSize - _hand.Count);
        _hand.AddRange(drawn);
        return drawn;
    }

    public List<Card> DrawDebug(int amount)
    {
        var drawn = _deck.Draw(amount);
        _hand.AddRange(drawn);
        return drawn;
    }
}
