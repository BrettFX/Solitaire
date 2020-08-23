using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Solitaire
{
    public class GameManager : MonoBehaviour
    {
        private static GameManager instance;
        public static GameManager Instance {
            get
            {
                return instance;
            }
        }

        public static bool DEBUG_MODE = true;

        public const int HOME_SCENE = 0;

        // Control the speed that cards are moved from one point to the next (higher = faster)
        public const float CARD_TRANSLATION_SPEED = 750.0f;

        public const float Z_OFFSET_DRAGGING = 70.0f;
        public const float FOUNDATION_Y_OFFSET = 30.0f;

        public static readonly string[] VALUE_REF =
        {
            "0", "Ace", "2", "3", "4", "5", "6", "7", "8", "9", "10", "JACK", "QUEEN", "KING"
        };

        public static readonly HashSet<string> PROHIBITED_DROP_LOCATIONS = new HashSet<string>
        {
            "Stock", "Talon"
        };

        public enum CardState
        {
            FACE_UP, FACE_DOWN
        };

        public enum CardSuit
        {
            HEARTS,
            DIAMONDS,
            CLUBS,
            SPADES,
            NONE
        };

        public enum CardSuitColor
        {
            RED,
            BLACK,
            NONE
        }

        public enum Sections
        {
            TABLEAU,
            STOCK,
            TALON,
            FOUNDATIONS
        };

        [Header("Set Up")]
        public GameObject tableau;
        public GameObject stock;
        public GameObject talon;
        public GameObject foundations;

        [Header("Utils")]
        public GameObject cardPrefab;
        public Text lblTimer;

        private Sprite[] m_cardSprites;
        private Dictionary<CardSuit, Sprite[]> m_cardSpritesMap;

        private Transform m_stockPile;
        private Transform m_talonPile;

        private SnapManager[] m_foundationSnapManagers;
        private SnapManager[] m_tableauSnapManagers;

        // Globally track if cards are being dragged to prevent multi-touch scenarios
        private ObjectDragger m_activeDragger = null;

        // Used to reset the timer
        private float m_timeBuffer = 0.0f;

        /**
         * Ensure this class remains a singleton instance
         * */
        void Awake()
        {
            // If the instance variable is already assigned...
            if (instance != null)
            {
                // If the instance is currently active...
                if (instance.gameObject.activeInHierarchy == true)
                {
                    // Warn the user that there are multiple Game Managers within the scene and destroy the old manager.
                    Debug.LogWarning("There are multiple instances of the Game Manager script. Removing the old manager from the scene.");
                    Destroy(instance.gameObject);
                }

                // Remove the old manager.
                instance = null;
            }

            // Assign the instance variable as the Game Manager script on this object.
            instance = GetComponent<GameManager>();
        }

        // Start is called before the first frame update
        void Start()
        {
            m_stockPile = stock.GetComponentInChildren<SnapManager>().transform;
            m_talonPile = talon.GetComponentInChildren<SnapManager>().transform;
            m_foundationSnapManagers = foundations.GetComponentsInChildren<SnapManager>();
            m_tableauSnapManagers = tableau.GetComponentsInChildren<SnapManager>();
            LoadCardSprites();
            SpawnStack();
        }

        private void Update()
        {
            if (!IsWinningState())
            {
                UpdateTimer();
            }
        }

        private bool IsWinningState()
        {
            int cardCountSum = 0;
            foreach (SnapManager snapManager in m_foundationSnapManagers)
            {
                cardCountSum += snapManager.GetCardCount();
            }

            // Game is won if the sum of all cards in the foundations is 52
            return cardCountSum == 52;
        }

        /**
        * 
        **/
        private void UpdateTimer()
        {
            float t = Time.timeSinceLevelLoad - m_timeBuffer; // time since scene loaded

            float milliseconds = (Mathf.Floor(t * 100) % 100); // calculate the milliseconds for the timer

            int seconds = (int)(t % 60); // return the remainder of the seconds divide by 60 as an int
            t /= 60; // divide current time y 60 to get minutes
            int minutes = (int)(t % 60); //return the remainder of the minutes divide by 60 as an int
            t /= 60; // divide by 60 to get hours
            int hours = (int)(t % 24); // return the remainder of the hours divided by 60 as an int

            lblTimer.text = string.Format("{0}:{1}:{2}.{3}", hours.ToString("00"), minutes.ToString("00"), seconds.ToString("00"), milliseconds.ToString("00"));
        }

        public void Reset()
        {
            // Set the time buffer to the time since level load so the timer starts back at zero
            // @see UpdateTimer
            m_timeBuffer = Time.timeSinceLevelLoad;
            SceneManager.LoadScene(HOME_SCENE);
        }

        /**
         * Register an object dragger to track if there are other draggers activated
         * through multi-touch
         */
        public void RegisterObjectDragger(ObjectDragger dragger)
        {
            m_activeDragger = dragger;
        }

        /*
         * Unregister an object dragger. Only unregisters if the object dragger
         * was the currently active object dragger.
         */
        public void UnregisterObjectDragger(ObjectDragger dragger)
        {
            // Only unregister if the object dragger is the active dragger
            if (m_activeDragger.Equals(dragger))
                m_activeDragger = null;
        }

        /**
         * Get the registered object dragger to compare for preventing
         * multi-touch scenarios.
         */
        public ObjectDragger GetRegisteredObjectDragger()
        {
            return m_activeDragger;
        }

        public Transform GetTalonPile()
        {
            return m_talonPile;
        }

        /**
         * Get the next avaiable/valid move for the card in question.
         * Scan through the placeable card locations in the Talons
         * and Foundations to determine if there is a valid snap location
         * to move the specified card.
         * 
         * The intended use cases for this function is when a user double clicks
         * a card to auto-complete a move or if the user desires a hint.
         * 
         * @param Card card the card to use to compare to any available drop
         *                  location in the Talons or Foundations.
         *                  
         * @return the Transform of the snap that represents the next
         *         available move for the card in question. Returns null if
         *         there are no available moves.
         */
        public Transform GetNextAvailableMove(Card card)
        {
            Transform nextMove = null;

            // First priority is the Foundations since the primary objective of the game is
            // to get all cards to the Foundations.
            foreach (SnapManager snapManager in m_foundationSnapManagers)
            {
                if (snapManager.IsValidMove(card))
                {
                    nextMove = snapManager.transform;
                    break;
                }
            }

            // Second priority is the tableau. Don't try to find a move if one has already been found
            // in the foundations
            if (!nextMove)
            {
                foreach (SnapManager snapManager in m_tableauSnapManagers)
                {
                    if (snapManager.IsValidMove(card))
                    {
                        nextMove = snapManager.transform;
                        break;
                    }
                }
            }

            return nextMove;
        }

        /**
         * Take all cards from talon and put them back in the stock
         */
        public void ReplinishStock()
        {
            SnapManager talonSnapManager = talon.GetComponentInChildren<SnapManager>();

            Card[] talonCards = talonSnapManager.GetCardSet(0);
            if (DEBUG_MODE) Debug.Log("Cards in talon:");
            
            // Need to iterate in reverse order so that the cards are drawn from the stock in the same order as before
            for (int i = talonCards.Length - 1; i >= 0; i--)
            {
                Card card = talonCards[i];

                // Remove from talon
                card.transform.parent = null;

                // Move the card position from the talon to the stock
                card.transform.position = new Vector3(
                    m_stockPile.position.x,
                    m_stockPile.position.y,
                    card.transform.position.z
                );

                // Rotate the card to be face down again
                CardState cardState = card.Flip(false);
                if (DEBUG_MODE)
                {
                    Debug.Log(card.value + " of " + card.suit + " is " + cardState);
                }

                // Add the card to the stock
                card.transform.parent = m_stockPile;

                // Flip the first card from the stock pile over to the talon automatically
                if (i == 0)
                {
                    card.MoveTo(m_talonPile);
                }
            }
        }

        private void LoadCardSprites()
        {
            // Load the card sprites from resources
            m_cardSprites = Resources.LoadAll<Sprite>("Sprites/playing_cards_spritesheet_01");
            if (DEBUG_MODE) Debug.Log("Loaded " + m_cardSprites.Length + " card sprites...");
            m_cardSpritesMap = new Dictionary<CardSuit, Sprite[]>();
            for (int i = 0; i < m_cardSprites.Length; i++)
            {
                Sprite sprite = m_cardSprites[i];
                string[] tokens = sprite.name.Split('_');
                int value = int.Parse(tokens[0]);
                string suitString = tokens[1];
                CardSuit suit;

                // Only process when value is between 0 and length of the card sprites
                if ((value - 1) < m_cardSprites.Length && (value - 1) >= 0)
                {
                    switch (suitString)
                    {
                        case "HEARTS":
                            suit = CardSuit.HEARTS;
                            break;
                        case "DIAMONDS":
                            suit = CardSuit.DIAMONDS;
                            break;
                        case "CLUBS":
                            suit = CardSuit.CLUBS;
                            break;
                        case "SPADES":
                            suit = CardSuit.SPADES;
                            break;
                        default:
                            suit = CardSuit.NONE;
                            break;
                    }

                    // Create the suit entry if it doesn't exist
                    if (!m_cardSpritesMap.ContainsKey(suit))
                    {
                        // 13 cards per suit
                        m_cardSpritesMap.Add(suit, new Sprite[13]);
                    }

                    // Value is not zero-indexed so we have to compensate for that
                    // Add the sprite to the respective suit entry and array slot
                    m_cardSpritesMap[suit][value - 1] = sprite;
                }
                else
                {
                    if (DEBUG_MODE) Debug.Log("Excluding " + sprite.name + " from the deck.");
                }
            }
        }

        private void SpawnStack()
        {
            // Generate the initial list of 52 cards first before shuffling
            List<CardTpl> deck = new List<CardTpl>();
            CardSuit[] cardSuits = new CardSuit[]
            {
                CardSuit.HEARTS, CardSuit.DIAMONDS, CardSuit.CLUBS, CardSuit.SPADES
            };

            int cardsPerSuit = 13;
            for (int i = 0; i < cardSuits.Length; i++)
            {
                for (int j = 0; j < cardsPerSuit; j++)
                {
                    CardTpl card = new CardTpl
                    {
                        value = j + 1,
                        suit = cardSuits[i] // Purposefully using 'i' to cycle through all cards before changing suit
                    };

                    // Add the card to the deck
                    deck.Add(card);
                }
            }

            // Shuffle the cards
            Shuffle(ref deck);

            // Spwan Tableau cards
            SpawnTableauCards(ref deck);

            // Spawn remaining cards to the stock
            SpawnStockCards(ref deck);
        }

        /**
         * Shuffle an array of values based on specified datatype.
         * @param T[] array the template based array to shuffle.
         * @return T[] the array representing the shuffled version of the input array.
         */
        public void Shuffle<T>(ref List<T> array)
        {
            for (int i = array.Count; i > 1; i--)
            {
                // Pick random element to swap.
                int j = Random.Range(0, array.Count - 1); // 0 <= j <= i-1
                                        // Swap.
                T tmp = array[j];
                array[j] = array[i - 1];
                array[i - 1] = tmp;
            }
        }

        private void SpawnTableauCards(ref List<CardTpl> deck)
        {
            // Iterate through the tableau snaps and spawn cards from deck to them
            SnapManager[] tableauSnapManagers = tableau.GetComponentsInChildren<SnapManager>();
            int zOffset = 1;
            float yOffset = 10.0f; // Face down cards will have a smaller y-offset
            for (int i = 0; i < tableauSnapManagers.Length; i++)
            {
                Transform stackTarget = tableauSnapManagers[i].transform;

                for (int j = 0; j < i + 1; j++)
                {
                    Card card = cardPrefab.GetComponent<Card>();
                    card.value = deck[0].value;
                    card.suit = deck[0].suit;

                    // Get the sprite renderer for the current front face of this card
                    SpriteRenderer cardSprite = card.frontFace.GetComponent<SpriteRenderer>();

                    // Set the sprite for the front face
                    cardSprite.sprite = m_cardSpritesMap[card.suit][card.value - 1];

                    Vector3 posOffset = new Vector3(
                       stackTarget.position.x,
                       stackTarget.position.y - (yOffset * j),
                       stackTarget.position.z - zOffset
                    );

                    // Spawn the card and add it to the respective tableau pile
                    GameObject spawnedCard = Instantiate(cardPrefab, posOffset, Quaternion.identity);
                    spawnedCard.name = VALUE_REF[card.value] + "_" + card.suit;
                    spawnedCard.transform.parent = stackTarget;

                    // Only make the last card flip face up
                    if (j + 1 == i + 1)
                    {
                        spawnedCard.GetComponent<Card>().Flip(false);
                    }

                    zOffset++;

                    // Remove the card reference from the deck and move to the next card
                    deck.RemoveAt(0);
                }

                zOffset = 1;
            }
        }

        private void SpawnStockCards(ref List<CardTpl> deck)
        {
            // Card spawn location is dependent on the location of the Stock parent
            Transform stackTarget = stock.GetComponentInChildren<SnapManager>().GetComponent<Transform>();
            if (DEBUG_MODE) Debug.Log("Stack target is " + stackTarget.tag + " at " + stackTarget.position);

            int zOffset = 1;
            while (deck.Count != 0)
            {
                Card card = cardPrefab.GetComponent<Card>();
                card.value = deck[0].value;
                card.suit = deck[0].suit;

                // Get the sprite renderer for the current front face of this card
                SpriteRenderer cardSprite = card.frontFace.GetComponent<SpriteRenderer>();

                // Set the sprite for the front face
                cardSprite.sprite = m_cardSpritesMap[card.suit][card.value - 1];

                Vector3 posOffset = new Vector3(
                    stackTarget.position.x,
                    stackTarget.position.y,
                    stackTarget.position.z - zOffset
                );

                // Spawn the card and add it to the stock
                GameObject spawnedCard = Instantiate(cardPrefab, posOffset, Quaternion.identity);
                spawnedCard.name = VALUE_REF[card.value] + "_" + card.suit;
                spawnedCard.transform.parent = stackTarget;
                zOffset++;

                // Remove the card reference from the deck and move to the next card
                deck.RemoveAt(0);
            }
        }
    }
}


