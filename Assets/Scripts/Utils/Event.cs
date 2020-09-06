using UnityEngine;

namespace Solitaire
{
    public class Event
    {
        public enum EventType
        {
            DRAW,
            TRANSLATE,
            FLIP,
            NONE
        };

        private EventType m_eventType = EventType.NONE;
        private Card[] m_cards;
        private Vector3 m_startPos = Vector3.zero;
        private Vector3 m_endPos = Vector3.zero;
        private Transform m_prevParent;
        private Transform m_nextParent;

        public void Reverse()
        {
            // Perform the reverse action of the event based on event type
            switch (m_eventType)
            {
                case EventType.FLIP:
                    Debug.Log("Reversing flip for " + m_cards[0]);
                    m_cards[0].Flip();
                    break;
                case EventType.TRANSLATE:
                    break;
                case EventType.DRAW:
                    break;
                default:
                    break;
            }
        }

        public void SetType(EventType eventType)
        {
            m_eventType = eventType;
        }

        public void SetCard(Card card)
        {
            m_cards = new Card[] { card };
        }

        public void SetCards(Card[] cards)
        {
            m_cards = cards;
        }

        public void SetStartPos(Vector3 startPos)
        {
            m_startPos = startPos;
        }

        public void SetEndPos(Vector3 endPos)
        {
            m_endPos = endPos;
        }

        public void SetPreviousParent(Transform prevParent)
        {
            m_prevParent = prevParent;
        }

        public void SetNextParent(Transform nextParent)
        {
            m_nextParent = nextParent;
        }

        public EventType GetEventType()
        {
            return m_eventType;
        }

        public Card[] GetCards()
        {
            return m_cards;
        }

        public Vector3 GetStartPos()
        {
            return m_startPos;
        }

        public Vector3 GetEndPos()
        {
            return m_endPos;
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
