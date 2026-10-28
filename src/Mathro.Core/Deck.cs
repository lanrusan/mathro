namespace Mathro.Core;

using System.Runtime.InteropServices;

public class Deck
{
    public void deckInitialize(List<Card> deck)
    {
        Console.WriteLine("Initializing the deck...");
        string[] baseCards ={"0","1","2","3",
                            "4","5","6","7",
                            "8","9","+","+",
                            "-","-","×","÷"};
        foreach (var card in baseCards)
        {
            if (card == "+" || card == "-" || card == "×" || card == "÷")
            {
                var newCard = new Card();
                newCard.addCard(card, "Operator");
                deck.Add(newCard);
                continue;
            }
            else {
                var newCard = new Card();
                newCard.addCard(card, "decimal");
                deck.Add(newCard);
                continue;
            }
        }
        Console.WriteLine("Deck initialized with the following cards:");
        foreach (var card in deck)
        {
            Console.WriteLine(card.value);
        }
    }

    public void DeckShuffle(List<Card> deck)
    {
        Console.WriteLine("Shuffling the deck...");
        Random.Shared.Shuffle(CollectionsMarshal.AsSpan(deck));
    }

    public void DeckDraw(List<Card> deck)
    {
        Console.WriteLine("Drawing a card from the deck...");
        List<Card> tempDeck = deck.ToList();
        List<Card> drawnCards = new List<Card>();
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
                Console.Write(card.value + " ");
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
