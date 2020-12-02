namespace Solitaire
{
    public class Event
    {
        public enum EventType
        {
            FLIP,
            REPLINISH,
            DEPLINISH,
            NONE
        };

        private EventType m_eventType = EventType.NONE;
        private Card[] m_cards;
        private SnapManager m_relativeSnapManager;

        public void Reverse()
        {   
            // Perform the reverse action of the event based on event type
            switch (m_eventType)
            {
                case EventType.FLIP:
                    // Need to temporarily lock snap manager so that the card isn't flipped back after reverse
                    m_relativeSnapManager.SetWaiting(true);
                    m_cards[0].Flip(false);
                    break;
                case EventType.REPLINISH:
                    GameManager.Instance.DeplinishStock(Move.MoveTypes.INCOGNITO);
                    break;
                case EventType.DEPLINISH:
                    GameManager.Instance.ReplinishStock(Move.MoveTypes.INCOGNITO);
                    break;
                default:
                    break;
            }
        }

        public void SetRelativeSnapManager(SnapManager snapManager)
        {
            m_relativeSnapManager = snapManager;
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

        public SnapManager GetRelativeSnapManager()
        {
            return m_relativeSnapManager;
        }

        public EventType GetEventType()
        {
            return m_eventType;
        }

        public Card[] GetCards()
        {
            return m_cards;
        }
    }
}
