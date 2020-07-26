using UnityEngine;

namespace Solitaire
{
    public class SnapManager : MonoBehaviour
    {
        private Card[] m_attachedCards;

        // Start is called before the first frame update
        void Start()
        {
            // As an initial step, check if there are any cards already attached to the snap
            m_attachedCards = gameObject.GetComponentsInChildren<Card>();

            //if (m_attachedCards.Length == 0)
            //{
            //    Debug.Log("No cards attached to " + gameObject.name);
            //}

            // Determine if there are some attached cards
            if (m_attachedCards.Length > 0)
            {
                // If there are then we need to iterate them to perform some preprocesing steps
                Debug.Log("Attached cards for " + gameObject.name);
                for (int i = 0; i < m_attachedCards.Length; i++)
                {
                    Card card = m_attachedCards[i];

                    // Need to turn off collision for all cards except for the last one in the stack
                    if (i != m_attachedCards.Length - 1)
                    {
                        card.GetComponent<MeshCollider>().enabled = false;
                    }

                    Debug.Log(card.transform.position + ": " + GameManager.VALUE_REF[card.value] + " of " + card.suit);
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            // Keep track of all the cards that are attached to this snap
        }
    }
}
