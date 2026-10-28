using System;
using System.Collections.Generic;
using System.Text;

namespace Mathro.Core
{
    public class Card
    {
        public string value;
        public string numType;

        // value - the string value of the card (0-9 for numbers, (+,-,/,*) and a specific value for operators)
        // numType - the type of number (e.g., "number", "operator")
        // stampType - the type of stamp (e.g., "circle", "square", "triangle")
        public void addCard(string Value, string NumType) { 
        
           value = Value;
           numType = NumType; 

        }

    }
}
