namespace Mathro.Core;

using System.Runtime.InteropServices;

public class Deck
{
    public void deckInitialize(List<string> deck)
    {
        Console.WriteLine("Initializing the deck...");
        string[] cards =    {"0","1","2","3",
                            "4","5","6","7",
                            "8","9","+","+",
                            "-","-","×","÷"};
        deck.AddRange(cards);
        Console.WriteLine("Deck initialized with the following cards:");
        foreach (var card in deck)
        {
            Console.WriteLine(card);
        }
    }

    public void DeckShuffle(List<string> deck)
    {
        Console.WriteLine("Shuffling the deck...");
        Random.Shared.Shuffle(CollectionsMarshal.AsSpan(deck));
    }

    public void DeckDraw(List<string> deck)
    {
        Console.WriteLine("Drawing a card from the deck...");
        List<string> tempDeck = deck.ToList();
        List<string> drawnCards = new List<string>();
        string choice = "";
        do {
            if (tempDeck.Count > 5)
            {
                for (int i = 0; i < 5; i++) //Draw 5 Cards
                {
                    drawnCards.Add(tempDeck[i]);
                }
                tempDeck.RemoveRange(0, 5);

            }
            else { 
                for (int i = 0; i < tempDeck.Count; i++)
                {
                    drawnCards.Add(tempDeck[i]);
                }
                tempDeck.Clear();
            }
            
            Console.WriteLine("Drawn cards:");
            foreach (var card in drawnCards)
            {
                Console.Write(card + " ");
            }
            if (tempDeck.Count == 0)
            {
                Console.WriteLine("\nNo more cards left in the deck.");
                break;
            }
            else {
            Console.WriteLine("Draw Again? (y/n)");
            drawnCards.Clear();
            choice = Console.ReadLine();
            Console.WriteLine();
            }
            
        }   while (tempDeck.Count != 0 && choice == "y");

    }

}
