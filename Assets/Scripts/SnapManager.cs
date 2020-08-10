using System.Collections.Generic;
using UnityEngine;
using static Solitaire.GameManager;

namespace Solitaire
{
    public class SnapManager : MonoBehaviour
    {
        public Sections belongsTo;
        private Card[] m_attachedCards;
        private MeshCollider m_snapCollider;
        private bool m_waiting = false;

        private void Start()
        {
            m_snapCollider = GetComponent<MeshCollider>();
        }

        // Update is called once per frame
        void Update()
        {
            // Keep the list of attached cards updated
            m_attachedCards = gameObject.GetComponentsInChildren<Card>();

            // Determine if there are some attached cards
            if (m_attachedCards.Length > 0)
            {
                // Need to turn off snap collision if there is at least one card on it
                // (fixes bug for clicking snaps instead of cards)
                if (m_snapCollider.enabled)
                {
                    m_snapCollider.enabled = false;
                }

                // If there are then we need to iterate them to perform some preprocesing steps
                for (int i = 0; i < m_attachedCards.Length; i++)
                {
                    Card card = m_attachedCards[i];
                    //card.SetStartParent(card.transform.parent);

                    // Need to set each card as non-stackable except for the last one in the stack
                    card.SetStackable(i == m_attachedCards.Length - 1);

                    // Need to flip the last card in the stack face up if it's face down (only applies to Tableau)
                    if (i == m_attachedCards.Length - 1 && belongsTo.Equals(Sections.TABLEAU) && !m_waiting)
                    {
                        // Only flip it face up if it's face down and the previous card move was valid
                        if (card.currentState.Equals(CardState.FACE_DOWN))
                        {
                            card.Flip(false);
                        }
                    }

                    // Normalize z-pos to ensure that z-values are consistent for each card in the stack
                    if (card.transform.position.z != -i)
                    {
                        card.transform.position = new Vector3(
                            card.transform.position.x,
                            card.transform.position.y,
                            -i
                        );
                    }

                    card.SetStartPos(card.transform.position);
                }
            }
            else
            {
                // Turn snap collision back on if there are no cards attached
                if (!m_snapCollider.enabled)
                {
                    m_snapCollider.enabled = true;
                }
            }
        }

        /**
         * Determine if the card to be placed on this snap is valid or not.
         * Handles checking specific parent that this snap is a part of (e.g., Foundation or
         * Tableau). Immediately determined as invalid if dropping on Stock or Talon.
         */
        public bool IsValidMove(Card card)
        {
            bool valid = false;
            switch(belongsTo)
            {
                case Sections.FOUNDATIONS:
                    valid = IsValidNextFoundationCard(card);
                    break;
                case Sections.TABLEAU:
                    valid = IsValidNextTableauCard(card);
                    break;
                default:
                    break;
            }

            return valid;
        }

        public void SetWaiting(bool wait)
        {
            m_waiting = wait;
        }

        /**
         * Determine if the next card to be placed on the foundation is valid or not.
         * Rules for next card to be placed in foundation stack:
         * 1) Card value must be exactly 1 value greater than current card value. If no cards, then value must be 1 (Ace).
         * 2) Suit must match the current card. If no cards then this rule doesn't apply.
         */
        private bool IsValidNextFoundationCard(Card nextCard)
        {
            bool valid;
            // Check if there are no cards
            if (m_attachedCards.Length == 0)
            {
                valid = nextCard.value == 1;
            }
            else
            {
                // Get the top card and validate
                Card currentCard = m_attachedCards[m_attachedCards.Length - 1];
                valid = (nextCard.value == currentCard.value + 1) && (nextCard.suit.Equals(currentCard.suit));
            }

            return valid;
        }

        /*
         * Determine if the next card to be placed on the respective tableau snap is valid or not.
         * Rules for next card to be placed in the tableau stack:
         * 1) Card value must be exactly 1 value less than current card value. If no cards, then value must be 13 (King).
         * 2) Suit must alternate in color from current card (e.g, if current card is red then the next card must be black
         *    and vice versa). If no cards, then this rule doesn't apply. 
         */
        private bool IsValidNextTableauCard(Card nextCard)
        {
            bool valid;
            // Check if thre are no cards
            if (m_attachedCards.Length == 0)
            {
                valid = nextCard.value == 13;
            }
            else
            {
                // Get the top card and validate
                Card currentCard = m_attachedCards[m_attachedCards.Length - 1];
                bool altColorSuit = GetSuitColor(currentCard.suit, true) == GetSuitColor(nextCard.suit, false);
                valid = (nextCard.value == currentCard.value - 1) && altColorSuit;
            }

            return valid;
        }

        private CardSuitColor GetSuitColor(CardSuit currentSuit, bool alternate)
        {
            CardSuitColor suitColor = CardSuitColor.NONE;
            switch (currentSuit)
            {
                case CardSuit.CLUBS:
                case CardSuit.SPADES:
                    // Color would be black; alternate to red if specified
                    suitColor = alternate ? CardSuitColor.RED : CardSuitColor.BLACK;
                    break;
                case CardSuit.HEARTS:
                case CardSuit.DIAMONDS:
                    // Color would be red; alternate to black if specified
                    suitColor = alternate ? CardSuitColor.BLACK : CardSuitColor.RED;
                    break;
                default:
                    break;
            }

            return suitColor;
        }

        public bool HasCard()
        {
            return m_attachedCards.Length != 0;
        }

        /**
         * Get a subset of cards from attached cards to the managed snap.
         * @param int startIndex the starting point in the list of cards to base the subset on.
         * @return Card[] the subset of cards in the attached cards list
         * */
        public Card[] GetCardSet(int startIndex)
        {
            // Default to returning the entire set of attached card if the start index is out of lower bounds
            if (startIndex <= 0)
            {
                return m_attachedCards;
            }

            // Default to last attached card if start index is out of upper bounds
            if (startIndex >= m_attachedCards.Length)
            {
                return new Card[1] { m_attachedCards[m_attachedCards.Length - 1] };
            }

            // Otherwise, we need to iterate the set of attached cards and return the subset from the starting index
            // to the end of the stack
            Card[] cardSet = new Card[m_attachedCards.Length - startIndex];
            for (int i = startIndex; i < m_attachedCards.Length; i++)
            {
                cardSet[i] = m_attachedCards[i];
            }

            return cardSet;
        }

        public Card[] GetCardSet(Card startingCard)
        {
            bool found = false;
            List<Card> cardSet = new List<Card>();

            // Need to find the starting index by finding the card
            foreach(Card card in m_attachedCards)
            {
                if (card.Equals(startingCard))
                {
                    found = true;
                }

                // Only start adding cards to the card set once we've identified a match (starting point)
                if (found)
                {
                    cardSet.Add(card);
                }
            }

            return cardSet.ToArray();
        }
    }
}
