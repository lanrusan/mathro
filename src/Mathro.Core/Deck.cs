using System.Runtime.InteropServices;

namespace Mathro.Core;

public enum CardCategory
{
    Number,
    Operator,
    // Modifier,
}

public class Card(CardCategory category, string value)
{
    public CardCategory Category = category;
    public string Value = value;
}

public class Deck
{
    private readonly List<Card> _allCards = [];
    private readonly List<Card> _drawPile = [];
    private readonly Random _random;

    public int Total => _allCards.Count;

    public int Remaining => _drawPile.Count;

    public Deck(Random? random = null)
    {
        _random = random ?? Random.Shared;

        foreach (var (op, count) in new[] { ("+", 5), ("-", 4), ("*", 3), ("/", 3) })
        {
            for (int i = 0; i < count; i++)
            {
                _allCards.Add(new Card(CardCategory.Operator, op));
            }
        }

        for (int copy = 0; copy < 2; copy++)
        {
            for (int n = 1; n < 10; n++)
            {
                _allCards.Add(new Card(CardCategory.Number, n.ToString()));
            }
        }

        for (int n = -9; n < 0; n++)
        {
            _allCards.Add(new Card(CardCategory.Number, n.ToString()));
        }

        for (decimal n = 0; n < 1; n += 0.1m)
        {
            _allCards.Add(new Card(CardCategory.Number, n.ToString()));
        }

        Reset();
    }

    public void Reset()
    {
        _drawPile.Clear();
        _drawPile.AddRange(_allCards);
        _random.Shuffle(CollectionsMarshal.AsSpan(_drawPile));
    }

    public List<Card> Draw(int amount)
    {
        var toDraw = Math.Clamp(amount, 0, _drawPile.Count);
        var startIndex = _drawPile.Count - toDraw;
        var drawn = _drawPile[startIndex..];
        _drawPile.RemoveRange(startIndex, toDraw);
        return drawn;
    }
}
