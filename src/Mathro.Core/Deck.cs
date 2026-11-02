using System.Runtime.InteropServices;

namespace Mathro.Core;

public enum CardCategory
{
    Number,
    Operator,
    Multiplier,
    Modifier,
}

public class Card(CardCategory category, string value)
{
    public CardCategory Category = category;
    public string Value = value;
}

public class Deck
{
    private List<Card> DeckFull;
    private List<Card> DeckUsed;

    public Deck()
    {
        DeckFull =
        [
            new Card(CardCategory.Operator, "+"),
            new Card(CardCategory.Operator, "+"),
            new Card(CardCategory.Operator, "+"),
            new Card(CardCategory.Operator, "+"),
            new Card(CardCategory.Operator, "-"),
            new Card(CardCategory.Operator, "-"),
            new Card(CardCategory.Operator, "-"),
            new Card(CardCategory.Operator, "-"),
            new Card(CardCategory.Operator, "*"),
            new Card(CardCategory.Operator, "*"),
            new Card(CardCategory.Operator, "/"),
            new Card(CardCategory.Operator, "/"),
        ];

        for (int i = 0; i < 3; i++)
        {
            for (int j = 1; j < 10; j++)
            {
                DeckFull.Add(new Card(CardCategory.Number, j.ToString()));
            }
        }

        Random.Shared.Shuffle(CollectionsMarshal.AsSpan(DeckFull));
        DeckUsed = DeckFull;
    }

    public void Reset()
    {
        DeckUsed = DeckFull;
    }

    public List<Card> Draw(int amount)
    {
        var toDraw = Math.Min(DeckUsed.Count, amount);
        var startIndex = DeckUsed.Count - toDraw;
        var drawnCards = DeckUsed[startIndex..];
        DeckUsed.RemoveRange(startIndex, toDraw);

        return drawnCards;
    }

    // public void DeckDraw(List<Card> deck)
    // {
    //     Console.WriteLine("Drawing a card from the deck...");
    //     List<Card> tempDeck = deck.ToList();
    //     List<Card> drawnCards = new List<Card>();
    //     string choice = "";
    //     do
    //     {
    //         if (tempDeck.Count > 5)
    //         {
    //             for (int i = 0; i < 5; i++) //Draw 5 Cards
    //             {
    //                 drawnCards.Add(tempDeck[i]);
    //             }
    //             tempDeck.RemoveRange(0, 5);
    //         }
    //         else
    //         {
    //             for (int i = 0; i < tempDeck.Count; i++)
    //             {
    //                 drawnCards.Add(tempDeck[i]);
    //             }
    //             tempDeck.Clear();
    //         }
    //
    //         Console.WriteLine("Drawn cards:");
    //         foreach (var card in drawnCards)
    //         {
    //             Console.Write(card.value + " ");
    //         }
    //         if (tempDeck.Count == 0)
    //         {
    //             Console.WriteLine("\nNo more cards left in the deck.");
    //             break;
    //         }
    //         else
    //         {
    //             Console.WriteLine("Draw Again? (y/n)");
    //             drawnCards.Clear();
    //             choice = Console.ReadLine();
    //             Console.WriteLine();
    //         }
    //     } while (tempDeck.Count != 0 && choice == "y");
    // }
}
