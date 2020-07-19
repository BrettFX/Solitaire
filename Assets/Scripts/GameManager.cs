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

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}


