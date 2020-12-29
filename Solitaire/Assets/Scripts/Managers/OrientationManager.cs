using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Solitaire
{
    public class OrientationManager : MonoBehaviour
    {
        public static OrientationManager Instance { get; private set; }

        // Threshold time in milliseconds allowed between screen orientation change events
        private const float ORIENTATION_CHANGE_TIME_THRESHOLD = 250.0f;

        [Header("References")]
        public GameObject landscape;
        public GameObject portrait;
        public CanvasScaler scaler;

        [Header("Resolution Configuration")]
        public Vector2 portraitRes;
        public Vector2 landscapeRes;

        private RectTransform m_rectTransform;
        private System.Diagnostics.Stopwatch m_stopwatch;

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

            m_stopwatch = new System.Diagnostics.Stopwatch();
            m_stopwatch.Start();

            SetOrientation();
        }

        

        /**
         * Detect the new screen orientation and toggle the visibility of the 
         * associated portrait or landscape game objects based on the new 
         * screen orientation. Also, configure the CanvasScaler to conform to the
         * new screen resolution.
         */
        private void SetOrientation()
        {
            // TODO add spam guard with stopwatch (only allow orientation change events every n second(s))
            if (m_stopwatch.ElapsedMilliseconds < ORIENTATION_CHANGE_TIME_THRESHOLD) return;

            if (m_rectTransform == null) return;
            bool verticalOrientation = m_rectTransform.rect.width < m_rectTransform.rect.height;

            float newX = verticalOrientation ? portraitRes.x : landscapeRes.x;
            float newY = verticalOrientation ? portraitRes.y : landscapeRes.y;
            Vector2 newRes = new Vector2(newX, newY);

            Debug.Log("Should set canvas scaler reference resolution to: " + newRes);

            // Set the canvas scaler reference resolution based on new orientation
            // scaler.referenceResolution.Set(newX, newY);
            scaler.referenceResolution = newRes;

            Debug.Log("Current canvas scaler reference resolution is: " + scaler.referenceResolution);

            portrait.SetActive(verticalOrientation);
            landscape.SetActive(!verticalOrientation);


        }

        /**
         * Event triggered by UnityEngine.EventSystems (UIBehaviour) on mobile
         * devices when the screen orientation changes.
         * 
         * Implementation invokes a custom handle function for when the orientation
         * changes to determine the new orientation and process accordingly.
         */
        private void OnRectTransformDimensionsChange()
        {
            SetOrientation();
        }

    }
}

