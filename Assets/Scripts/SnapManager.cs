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
                //Debug.Log("Attached cards for " + gameObject.name);
                for (int i = 0; i < m_attachedCards.Length; i++)
                {
                    Card card = m_attachedCards[i];

                    // Need to set each card as non-stackable except for the last one in the stack
                    card.SetStackable(i == m_attachedCards.Length - 1);
                    //if (i != m_attachedCards.Length - 1)
                    //{
                    //    //MeshCollider collider = card.GetComponent<MeshCollider>();
                    //    //if (collider.enabled)
                    //    //    collider.enabled = false;

                    //    card.SetStackable(false);
                    //}

                    //Debug.Log(card.transform.position + ": " + GameManager.VALUE_REF[card.value] + " of " + card.suit);
                }
            }
        }
    }
}
