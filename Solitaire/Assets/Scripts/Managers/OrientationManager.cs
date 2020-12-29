using UnityEngine;
using UnityEngine.EventSystems;

namespace Solitaire
{
    public class OrientationManager : MonoBehaviour
    {
        public static OrientationManager Instance { get; private set; }

        private RectTransform m_rectTransform;
        public GameObject landscape;
        public GameObject portrait;

        private void Awake()
        {
            // If the instance variable is already assigned...
            if (Instance != null)
            {
                // If the instance is currently active...
                if (Instance.gameObject.activeInHierarchy == true)
                {
                    // Warn the user that there are multiple Game Managers within the scene and destroy the old manager.
                    Debug.LogWarning("There are multiple instances of the " + this + " script. Removing the old manager from the scene.");
                    Destroy(Instance.gameObject);
                }

                // Remove the old manager.
                Instance = null;
            }

            // Assign the instance variable as the Game Manager script on this object.
            Instance = GetComponent<OrientationManager>();

            m_rectTransform = GetComponent<RectTransform>();
            SetOrientation();
        }

        /**
         * 
         */
        private void SetOrientation()
        {
            if (m_rectTransform == null) return;
            bool verticalOrientation = m_rectTransform.rect.width < m_rectTransform.rect.height;
            portrait.SetActive(verticalOrientation);
            landscape.SetActive(!verticalOrientation);
        }

        private void OnRectTransformDimensionsChange()
        {
            Debug.Log("Detected orientation change.");
            SetOrientation();
        }

    }
}

