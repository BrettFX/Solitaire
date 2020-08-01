using System.Collections.Generic;
using UnityEngine;

namespace Solitaire
{
    public class SnapManager : MonoBehaviour
    {
        private Card[] m_attachedCards;

        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            // Keep the list of attached cards updated
            m_attachedCards = gameObject.GetComponentsInChildren<Card>();

            //if (m_attachedCards.Length == 0)
            //{
            //    Debug.Log("No cards attached to " + gameObject.name);
            //}

            // Determine if there are some attached cards
            if (m_attachedCards.Length > 0)
            {
                // If there are then we need to iterate them to perform some preprocesing steps
                for (int i = 0; i < m_attachedCards.Length; i++)
                {
                    Card card = m_attachedCards[i];

                    // Need to set each card as non-stackable except for the last one in the stack
                    card.SetStackable(i == m_attachedCards.Length - 1);

                    // TODO consider normalizing the positons to ensure that the y and z values
                    // are consistent for each card in the stack

                }
            }
        }

        /**
         * Iterate through the list of cards attached to this snap and normalize
         * the z-value of the positions to ensure that each card is properly stacked on
         * one another. The purpose of this function is to set things back to normal after
         * animations.
         */
        public void NormalizeCardPositions()
        {
            int zOffset = 1;
            foreach (Card card in m_attachedCards)
            {
                card.transform.position = new Vector3(
                    card.transform.position.x,
                    card.transform.position.y,
                    -zOffset
                );

                zOffset++;
            }
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
            if (startIndex < 0)
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
