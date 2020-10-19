﻿using System.Collections.Generic;
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

        public const string MASTER_VOL_KEY = "MasterVolume";
        public const string MUSIC_VOL_KEY = "MusicVolume";
        public const string SFX_VOL_KEY = "SFXVolume";

        [Header("Settings Pages")]
        public GameObject mainSettingsPage;
        public GameObject audioPage;
        public GameObject statsPage;

        [Header("Audio Assets")]
        public AudioSource cardSetSound;
        public AudioSource music;

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
            // Load saved settings if they exist
            LoadSettings();

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

        /**
         * Load previously saves settings from player prefs. If any settings
         * do not exist from player prefs for the slider values then the default
         * value is 1.0f.
         */
        private void LoadSettings()
        {
            sldMasterVol.value = PlayerPrefs.GetFloat(MASTER_VOL_KEY, 1.0f) * 100.0f;
            OnAudioSliderChange(sldMasterVol);

            sldMusicVol.value = PlayerPrefs.GetFloat(MUSIC_VOL_KEY, 1.0f) * 100.0f;
            OnAudioSliderChange(sldMusicVol);

            sldSfxVol.value = PlayerPrefs.GetFloat(SFX_VOL_KEY, 1.0f) * 100.0f;
            OnAudioSliderChange(sldSfxVol);
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
                // Save to player prefs
                PlayerPrefs.SetFloat(MASTER_VOL_KEY, m_masterVol);
                PlayerPrefs.SetFloat(MUSIC_VOL_KEY, m_musicVol);
                PlayerPrefs.SetFloat(SFX_VOL_KEY, m_sfxVol);
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

            // Need to normalize to value between 0 and 1
            float newValue = slider.value > 1.0 ? slider.value / 100.0f : slider.value;
            
            if (slider.CompareTag("MasterVolume"))
            {
                m_masterVol = newValue;
                AudioListener.volume = m_masterVol;
            }
            else if (slider.CompareTag("MusicVolume"))
            {
                m_musicVol = newValue;
                music.volume = m_musicVol;
            }
            else if (slider.CompareTag("SFXVolume"))
            {
                m_sfxVol = newValue;
                cardSetSound.volume = m_sfxVol;
            }
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

