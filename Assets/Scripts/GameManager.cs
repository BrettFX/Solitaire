using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        // Control the speed that cards are moved from one point to the next
        public const float CARD_TRANSLATION_SPEED = 500.0f;

        public const float Z_OFFSET = 70.0f;

        public static readonly string[] VALUE_REF =
        {
            "0", "Ace", "2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King"
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

        [Header("Template")]
        public GameObject cardPrefab;

        private Sprite[] m_cardSprites;
        private Dictionary<CardSuit, Sprite[]> m_cardSpritesMap;

        private Transform m_stockPile;
        private Transform m_talonPile;

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
            LoadCardSprites();
            SpawnStack();
        }

        public Transform GetTalonPile()
        {
            return m_talonPile;
        }

        /**
         * Take all cards from talon and put them back in the stock
         */
        public void ReplinishStock()
        {
            SnapManager talonSnapManager = talon.GetComponentInChildren<SnapManager>();

            Card[] talonCards = talonSnapManager.GetCardSet(0);
            Debug.Log("Cards in talon:");
            
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
            }
        }

        private void LoadCardSprites()
        {
            // Load the card sprites from resources
            m_cardSprites = Resources.LoadAll<Sprite>("Sprites/playing_cards_spritesheet_01");
            Debug.Log("Loaded " + m_cardSprites.Length + " card sprites...");
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
                    Debug.Log("Excluding " + sprite.name + " from the deck.");
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
            // TODO iterate through the tableau snaps and spawn cards from deck to them
        }

        private void SpawnStockCards(ref List<CardTpl> deck)
        {
            // Card spawn location is dependent on the location of the Stock parent
            Transform stackTarget = stock.GetComponentInChildren<SnapManager>().GetComponent<Transform>();
            Debug.Log("Stack target is " + stackTarget.tag + " at " + stackTarget.position);

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
                spawnedCard.name = cardPrefab.name + "_" + zOffset;
                spawnedCard.transform.parent = stackTarget;
                zOffset++;

                // Remove the card reference from the deck and move to the next card
                deck.RemoveAt(0);
            }
        }
    }
}


