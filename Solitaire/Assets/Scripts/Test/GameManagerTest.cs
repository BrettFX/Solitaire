using UnityEngine;
using UnityEngine.UI;

namespace Solitaire
{
    public class GameManagerTest : MonoBehaviour
    {
        [Header("Card Flip Animation Unit Test")]
        public Card[] testCards;

        [Header("Card Prefab Unit Test")]
        public GameObject cardPrefab;

        private GameObject m_spawnedCardObj;

        private bool m_portraitOrientation;

        public void Start()
        {
            if (testCards != null && testCards.Length > 0 && testCards[0].isActiveAndEnabled)
                InstantiateCardTest();

            m_portraitOrientation = OrientationManager.Instance.GetOrientation().Equals(OrientationManager.Orientations.PORTRAIT);
        }

        public void SimulateAutoWin(Toggle toggle)
        {
            Debug.Log("Turning " + (toggle.isOn ? "on" : "off") + " auto win state...");
            SimulateAutoWin(toggle.isOn);
        }

        /**
         * Find all object dragger instances and enable or disable them based
         * on the specified toggle flag.
         */
        private void SimulateAutoWin(bool enabled)
        {
            GameManager.Instance.SetDoingAutoWin(enabled);
        }

        private void InstantiateCardTest()
        {
            Vector3 pos = new Vector3(
                testCards[0].transform.position.x - 100,
                0,
                -1
            );
            m_spawnedCardObj = Instantiate(cardPrefab, pos, Quaternion.identity);
            m_spawnedCardObj.name = "Test_Card_Prefab_Instance";

            // Add the card animator to the card
            Card spawnedCard = m_spawnedCardObj.GetComponent<Card>();
            spawnedCard.Flip();
        }

        public void FlipCardTest()
        {
            foreach(Card card in testCards)
            {
                card.Flip();
            }

            m_spawnedCardObj.GetComponent<Card>().Flip();
        }

        /**
         * Toggle between scaling for portrait and landscape screen orientations
         */
        public void ToggleGameObjectsScaleTest()
        {
            m_portraitOrientation = !m_portraitOrientation;
            OrientationManager.Orientations newOrientation = m_portraitOrientation ?
                                                             OrientationManager.Orientations.PORTRAIT :
                                                             OrientationManager.Orientations.LANDSCAPE;
            GameManager.Instance.RescaleGameObjectsByOrientation(newOrientation);
        }
    }
}

