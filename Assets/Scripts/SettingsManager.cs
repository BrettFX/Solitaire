using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Solitaire
{
    public class SettingsManager : MonoBehaviour
    {
        private static SettingsManager instance;
        public static SettingsManager Instance
        {
            get
            {
                return instance;
            }
        }

        public struct SettingsPage
        {
            public GameObject currPage;
            public GameObject nextPage;
        }

        [Header("Settings Pages")]
        public GameObject mainSettingsPage;
        public GameObject audioPage;
        public GameObject statsPage;

        [Header("Audio Sliders")]
        public Slider sldMasterVol;
        public Slider sldMusicVol;
        public Slider sldSfxVol;


        [Header("Settings Lookup")]
        public List<Button> drivingButtons = new List<Button>();
        public List<GameObject> currPages = new List<GameObject>();
        public List<GameObject> nextPages = new List<GameObject>();

        private Dictionary<Button, SettingsPage> m_settingsPagesLookup;

        private Slider[] m_settingsSliders;

        private float m_masterVol;
        private float m_musicVol;
        private float m_sfxVol;

        private void Awake()
        {
            // If the instance variable is already assigned...
            if (instance != null)
            {
                // If the instance is currently active...
                if (instance.gameObject.activeInHierarchy == true)
                {
                    // Warn the user that there are multiple Game Managers within the scene and destroy the old manager.
                    Debug.LogWarning("There are multiple instances of the Settings Manager script. Removing the old manager from the scene.");
                    Destroy(instance.gameObject);
                }

                // Remove the old manager.
                instance = null;
            }

            // Assign the instance variable as the Game Manager script on this object.
            instance = GetComponent<SettingsManager>();
        }

        // Start is called before the first frame update
        void Start()
        {
            m_settingsSliders = new Slider[]
            {
                sldMasterVol,
                sldMusicVol,
                sldSfxVol
            };

            // Iterate through settings sliders and set the percent label accordingly
            // TODO implement player prefs to get initial values
            foreach (Slider slider in m_settingsSliders)
            {
                SetSliderPercentLabel(slider);
            }

            // Build settings pages lookup
            m_settingsPagesLookup = new Dictionary<Button, SettingsPage>();
            for (int i = 0; i < drivingButtons.Count; i++)
            {
                SettingsPage settingsPage = new SettingsPage
                {
                    currPage = currPages[i],
                    nextPage = nextPages[i]
                };

                m_settingsPagesLookup.Add(drivingButtons[i], settingsPage);
            }

        }

        // Update is called once per frame
        void Update()
        {

        }

        private void SetSliderPercentLabel(Slider slider)
        {
            TextMeshProUGUI txtPercent = slider.GetComponentInChildren<TextMeshProUGUI>();
            txtPercent.text = (slider.value <= 1.0 ? slider.value * 100 : slider.value) + "%";
        }

        public void CloseAudioSettings(bool save)
        {
            if (save)
            {
                // TODO save to player prefs
            }

            audioPage.SetActive(false);
            mainSettingsPage.SetActive(true);
        }

        public void CloseStatsSettings()
        {
            statsPage.SetActive(false);
            mainSettingsPage.SetActive(true);
        }

        public void OnAudioSliderChange(Slider slider)
        {
            SetSliderPercentLabel(slider);
        }

        /**
         * Open the next settings page based on the setting button that was clicked.
         * The current page will be hidden and the next page will be shown.
         */
        public void OpenNextSettingsPage(Button drivingButton)
        {
            if (m_settingsPagesLookup.ContainsKey(drivingButton))
            {
                SettingsPage settingsPage = m_settingsPagesLookup[drivingButton];
                settingsPage.currPage.SetActive(false);
                settingsPage.nextPage.SetActive(true);
            }
        }
    }
}

