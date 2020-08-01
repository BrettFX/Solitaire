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

        public static readonly string[] VALUE_REF =
        {
            "0", "Ace", "2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King"
        };

        public static readonly HashSet<string> PROHIBITED_DROP_LOCATIONS = new HashSet<string>
        {
            "Stock", "Tableau"
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
            SPADES
        };

        [Header("Set Up")]
        public GameObject tableau;
        public GameObject stock;
        public GameObject talon;
        public GameObject foundations;

        [Header("Template")]
        public GameObject cardPrefab;

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
            m_talonPile = talon.GetComponentInChildren<SnapManager>().transform;
            SpawnStack();
        }

        // Update is called once per frame
        void Update()
        {

        }

        public Transform GetTalonPile()
        {
            return m_talonPile;
        }

        private void SpawnStack()
        {
            // Card spawn location is dependent on the location of the Stock parent
            Transform stackTarget = stock.GetComponentInChildren<SnapManager>().GetComponent<Transform>();
            Debug.Log("Stack target is " + stackTarget.tag + " at " + stackTarget.position);

            // Generate the initial list of 52 cards first before shuffling
            Card[] deck = new Card[52];
            CardSuit[] cardSuits = new CardSuit[]
            {
                CardSuit.CLUBS, CardSuit.DIAMONDS, CardSuit.HEARTS, CardSuit.SPADES
            };

            int zOffset = 1;
            for (int i = 0; i < 13; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    Card card = cardPrefab.GetComponent<Card>();
                    card.value = i + 1;
                    card.suit = cardSuits[j];
                    Vector3 posOffset = new Vector3(
                        stackTarget.position.x,
                        stackTarget.position.y,
                        stackTarget.position.z - zOffset
                    );

                    // Spawn the card and add it to the stock
                    GameObject spawnedCard = Instantiate(cardPrefab, posOffset, Quaternion.identity);
                    Vector3 rot = spawnedCard.transform.eulerAngles;
                    rot = new Vector3(rot.x, rot.y + 180, rot.z);
                    spawnedCard.transform.rotation = Quaternion.Euler(rot);
                    spawnedCard.transform.parent = stackTarget;
                    zOffset++;
                }
            }
        }
    }
}


