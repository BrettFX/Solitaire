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

        public enum Orientations
        {
            PORTRAIT,
            LANDSCAPE,
            UNKNOWN
        };

        [Header("References")]
        public CanvasScaler scaler;

        [Header("Resolution Configuration")]
        public Vector2 portraitRes;
        public Vector2 landscapeRes;

        private Orientations m_currentOrientation = Orientations.UNKNOWN;

        /**
         * IMPORTANT: Attach this script to an empty game object within a Canvas and
         * ensure that the RectTransform is configured to stretch about the
         * x and y axes. Set the Left, Top, Pos Z, Right, and Bottom should all
         * to 0 so that the RectTransform stretches to fit the screen.
         */
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

            HandleOrientationChange();
        }

        public void SetOrientation(Orientations orientation)
        {
            m_currentOrientation = orientation;
        }

        public Orientations GetOrientation()
        {
            return m_currentOrientation;
        }

        /**
         * Detect the new screen orientation and toggle the visibility of the 
         * associated portrait or landscape game objects based on the new 
         * screen orientation. Also, configure the CanvasScaler to conform to the
         * new screen resolution.
         */
        private void HandleOrientationChange()
        {
            if (m_stopwatch.IsRunning)
            {
                // Add spam guard with stopwatch (only allow orientation change events every n second(s))
                if (m_stopwatch.ElapsedMilliseconds < ORIENTATION_CHANGE_TIME_THRESHOLD) return;
            }

            m_stopwatch.Reset();
            m_stopwatch.Start();

            if (m_rectTransform == null) return;

            bool verticalOrientation = m_rectTransform.rect.width < m_rectTransform.rect.height;
            m_currentOrientation = verticalOrientation ? Orientations.PORTRAIT : Orientations.LANDSCAPE;

            float newX = verticalOrientation ? portraitRes.x : landscapeRes.x;
            float newY = verticalOrientation ? portraitRes.y : landscapeRes.y;
            Vector2 newRes = new Vector2(newX, newY);

            // Set the canvas scaler reference resolution based on new orientation
            scaler.referenceResolution = newRes;

            // Notify game manager of orientation change
            // TODO need to ensure that objects are properly scaled based on starting orientation
            // Game manager is only null when this function is invoked from the OnRectTransformDimensionsChange function.
            if (GameManager.Instance != null)
                GameManager.Instance.RescaleGameObjectsByOrientation(m_currentOrientation);
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
            // Initialize the stop watch if it hasn't been initalized yet
            if (m_stopwatch == null)
            {
                m_stopwatch = new System.Diagnostics.Stopwatch();

                // Stop and reset to prevent from missing first frame
                m_stopwatch.Reset(); 
            }

            // Initialize the rect transform if it hasn't been initialized yet
            if (m_rectTransform == null)
            {
                m_rectTransform = GetComponent<RectTransform>();
            }

            HandleOrientationChange();
        }

    }
}

