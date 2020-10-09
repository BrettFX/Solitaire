using System.Collections.Generic;
using UnityEngine;

namespace Solitaire
{
    public class Move
    {
        public enum MoveTypes
        {
            UNDO,
            REDO,
            NORMAL,
            INCOGNITO
        };

        private Card m_topCard;
        private Card[] m_cards;
        private readonly List<Event> m_events;
        private Transform m_prevParent;
        private Transform m_nextParent;
        private bool m_special = false;

        public Move()
        {
            m_events = new List<Event>();
        }

        public void SetSpecial(bool special)
        {
            m_special = special;
        }

        public bool IsSpecial()
        {
            return m_special;
        }

        public void SetCards(Card[] cards)
        {
            m_cards = cards;
            m_topCard = m_cards[0];
        }

        public void AddEvent(Event e)
        {
            m_events.Add(e);
        }

        public List<Event> GetEvents()
        {
            return m_events;
        }

        public void SetPreviousParent(Transform prevParent)
        {
            m_prevParent = prevParent;
        }

        public void SetNextParent(Transform nextParent)
        {
            m_nextParent = nextParent;
        }

        public Card GetTopCard()
        {
            return m_topCard;
        }

        public Card[] GetCards()
        {
            return m_cards;
        }

        public Transform GetPreviousParent()
        {
            return m_prevParent;
        }

        public Transform GetNextParent()
        {
            return m_nextParent;
        }
    }
}
