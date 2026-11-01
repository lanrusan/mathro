using System;
using System.Collections.Generic;
using System.Data;

namespace Mathro.Core
{
    public enum CardCategory
    {
        Number,
        Operator,
        Multiplier,
        Modifier
    }

    public class PlayCard
    {
        public CardCategory Category { get; set; }
        public string Value { get; set; }
    }

    public class RoundController
    {
        public int CurrentRound { get; private set; }
        public int PlaysRemaining { get; private set; }
        public double AccumulatedScore { get; private set; }
        public double TargetScore { get; private set; }

        public void StartRound(int roundNumber, int allowedPlays)
        {
            CurrentRound = roundNumber;
            PlaysRemaining = allowedPlays;
            AccumulatedScore = 0;

            
            TargetScore = 500 * Math.Pow(1.5, roundNumber - 1);
        }

        public bool PlayHand(List<PlayCard> selectedCards)
        {
            if (PlaysRemaining <= 0)
            {
                return false;
            }
            if (selectedCards.Count > 5)
            {
                throw new ArgumentException("Maximum of 5 cards can be played per hand.");
            }

            PlaysRemaining--;

            string equationString = "";
            double totalMultiplier = 1;

            foreach (var card in selectedCards)
            {
                if (card.Category == CardCategory.Number || card.Category == CardCategory.Operator)
                {
                    
                    string token = card.Value.ToLower() == "x" ? "*" : card.Value;
                    equationString += token;
                }
                else if (card.Category == CardCategory.Multiplier)
                {
                    totalMultiplier *= double.Parse(card.Value);
                }
            }

            double baseScore = EvaluateMathString(equationString);
            double finalPlayScore = baseScore * totalMultiplier;

            AccumulatedScore += finalPlayScore;

            return HasMetTarget();
        }

        private double EvaluateMathString(string equation)
        {
            try
            {
                DataTable dt = new DataTable();
                var result = dt.Compute(equation, "");
                return Convert.ToDouble(result);
            }
            catch
            {
                return 0;
            }
        }

        public bool HasMetTarget()
        {
            return AccumulatedScore >= TargetScore;
        }
    }
}