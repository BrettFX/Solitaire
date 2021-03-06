﻿using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Solitaire
{
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        public struct SettingsPage
        {
            public GameObject currPage;
            public GameObject nextPage;
        }

        public struct AnimatorTriggerRef
        {
            public string trigger;
            public Animator animator;
            public bool animate;

            /**
             * Initialize this AnimatorTriggerRef instance.
             * 
             * @param Animator animator the animator to reference and invoke triggers on.
             */
            public AnimatorTriggerRef Init(Animator animator)
            {
                this.animator = animator;
                trigger = "";
                animate = true;
                return this;
            }

            /**
             * Invoke the associated trigger for this animator reference.
             * 
             * Animate if desired to invoke the trigger from first key frame.
             * Otherwise, play the animation from the last key frame, skipping
             * the animation.
             */
            public void DoTrigger()
            {
                if (animate)
                    animator.SetTrigger(trigger);
                else
                    animator.Play(trigger, 0, 1.0f);
            }
        }

        public const string MASTER_VOL_KEY = "MasterVolume";
        public const string MUSIC_VOL_KEY = "MusicVolume";
        public const string SFX_VOL_KEY = "SFXVolume";
        public const string AUTO_COMPLETE_TRIGGER_KEY = "AutoCompleteTrigger";
        public const string TIMER_VISIBILITY_KEY = "TimerVisibility";

        public const int AUTO_COMPLETE_SINGLE_TAP = 0;
        public const int AUTO_COMPLETE_DOUBLE_TAP = 1;

        // Constitutes the total amount of time after last event to wait before invoking animations
        public const float TIMER_ANIM_LISTEN_THRESHOLD = 500.0f; // In milliseconds

        [Header("Settings Pages")]
        public GameObject mainSettingsPage;
        public GameObject winSettingsPage;
        public GameObject gameplayPage;
        public GameObject audioPage;
        public GameObject statsPage;

        [Header("Audio Assets")]
        public AudioSource winSound;
        public AudioSource gearSound;
        public AudioSource cardSetSound;
        public AudioClip cardSetSoundClip;
        public AudioSource cardFlipSound;
        public AudioClip cardFlipSoundClip;
        public AudioSource clickSound;
        public AudioSource sfxTestSource; // Used for playing sound effect while adjusting sfx volume
        public AudioSource[] sfxSources;
        private AudioSource music; // Need to treat as singleton

        [Header("Audio Sliders")]
        public Slider sldMasterVol;
        public Slider sldMusicVol;
        public Slider sldSfxVol;


        [Header("Settings Lookup")]
        public List<Button> drivingButtons = new List<Button>();
        public List<GameObject> currPages = new List<GameObject>();
        public List<GameObject> nextPages = new List<GameObject>();

        [Header("Animators")]
        public Animator timerAnimator;
        public Animator actionBarAnimator;

        [Header("Miscellaneous")]
        public GameObject lblHighScoreNotification;
        public TMP_Dropdown dpnAutoCompleteTrigger;
        public Slider sldTimerLabel;

        private Dictionary<Button, SettingsPage> m_settingsPagesLookup;

        private Slider[] m_settingsSliders;

        private AnimatorTriggerRef m_timerAnimTriggerRef;
        private AnimatorTriggerRef m_actionBarAnimTriggerRef;

        private float m_masterVol;
        private float m_musicVol;
        private float m_sfxVol;

        private int m_autoCompleteTriggerCode;
        private bool m_timerVisible;

        private bool m_loadingSettings = false;

        private System.Diagnostics.Stopwatch m_timerAnimListenStopwatch;

        /**
        * Apply singleton logic to this SettingsManager instance
        */
        private void Awake()
        {
            // If the instance variable is already assigned...
            if (Instance != null)
            {
                // If the instance is currently active...
                if (Instance.gameObject.activeInHierarchy == true)
                {
                    // Warn the user that there are multiple Game Managers within the scene and destroy the old manager.
                    Debug.LogWarning("There are multiple instances of the Settings Manager script. Removing the old manager from the scene.");
                    Destroy(Instance.gameObject);
                }

                // Remove the old manager.
                Instance = null;
            }

            // Assign the instance variable as the Game Manager script on this object.
            Instance = GetComponent<SettingsManager>();
        }

        /**
        * Start is called before the first frame update
        */
        void Start()
        {
            m_timerAnimListenStopwatch = new System.Diagnostics.Stopwatch();

            // Initialize timer and action bar animation trigger references
            m_timerAnimTriggerRef = new AnimatorTriggerRef().Init(timerAnimator);
            m_actionBarAnimTriggerRef = new AnimatorTriggerRef().Init(actionBarAnimator);

            // Treat music audio source as singleton
            music = GameObject.FindGameObjectWithTag("Music").GetComponent<AudioSource>();

            // Load saved settings if they exist
            LoadSettings();

            m_settingsSliders = new Slider[]
            {
                sldMasterVol,
                sldMusicVol,
                sldSfxVol
            };

            // Iterate through settings sliders and set the percent label accordingly
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
        * 
        */
        private void Update()
        {
            // Run animation event once time threshold has been met
            if (m_timerAnimListenStopwatch.IsRunning)
            {
                if (m_timerAnimListenStopwatch.ElapsedMilliseconds >= TIMER_ANIM_LISTEN_THRESHOLD)
                {
                    if (GameManager.DEBUG_MODE)
                    {
                        Debug.Log("Invoking animation after waiting for " +
                                  (TIMER_ANIM_LISTEN_THRESHOLD / 1000.0f) +
                                  " second(s).");
                    }

                    // Invoke the target animations
                    m_timerAnimTriggerRef.DoTrigger();
                    m_actionBarAnimTriggerRef.DoTrigger();

                    // Stop the stopwatch and reset it
                    m_timerAnimListenStopwatch.Stop();
                    m_timerAnimListenStopwatch.Reset();
                }
            }

            // Keep the volume settings up to date
            AudioListener.volume = sldMasterVol.value / 100.0f;
            music.volume = sldMusicVol.value / 100.0f;
            foreach (AudioSource sfxSource in sfxSources)
            {
                sfxSource.volume = sldSfxVol.value / 100.0f;
            }
        }

        /**
         * Load previously saves settings from player prefs. If any settings
         * do not exist from player prefs for the slider values then the default
         * value is 1.0f.
         */
        public void LoadSettings()
        {
            m_loadingSettings = true;
            sldMasterVol.value = PlayerPrefs.GetFloat(MASTER_VOL_KEY, 1.0f) * 100.0f;
            OnAudioSliderChange(sldMasterVol);

            sldMusicVol.value = PlayerPrefs.GetFloat(MUSIC_VOL_KEY, 1.0f) * 100.0f;
            OnAudioSliderChange(sldMusicVol);

            sldSfxVol.value = PlayerPrefs.GetFloat(SFX_VOL_KEY, 1.0f) * 100.0f;
            OnAudioSliderChange(sldSfxVol);

            // Double tap is the default (1)
            m_autoCompleteTriggerCode = PlayerPrefs.GetInt(AUTO_COMPLETE_TRIGGER_KEY, 1);
            dpnAutoCompleteTrigger.SetValueWithoutNotify(m_autoCompleteTriggerCode);

            m_timerVisible = PlayerPrefs.GetInt(TIMER_VISIBILITY_KEY, 1) == 1;
            sldTimerLabel.SetValueWithoutNotify(m_timerVisible ? 1 : 0);

            // Toggle visibility of timer as needed (don't animate)
            HandleTimerVisibility(false);

            m_loadingSettings = false;
        }

        /**
        * 
        */
        private void SetSliderPercentLabel(Slider slider)
        {
            TextMeshProUGUI txtPercent = slider.GetComponentInChildren<TextMeshProUGUI>();
            txtPercent.text = (slider.value < 1.0 ? slider.value * 100 : slider.value) + "%";
        }

        /**
         * Toggle the visibility of the timer label based on the current state
         * of the timer slider toggle.
         * 
         * @param Slider timerLblSlider the slider that acts as a toggle between 1 and 0
         */
        public void OnTimerVisibilityChanged(Slider timerLblSlider)
        {
            m_timerVisible = timerLblSlider.value == 1;
            if (GameManager.DEBUG_MODE) Debug.Log("Timer lable visibility set to " + m_timerVisible);

            // Handle timer label animation
            HandleTimerVisibility();
        }

        /**
         * Invoke the appropriate timer visibility animation based on the current
         * value of the timer visible flag.
         * 
         * @param bool animate whether to plan to animate or not once the time elapsed since
         *                     this function was invoked meets the specified threshold time
         *                     in milliseconds.
         */
        private void HandleTimerVisibility(bool animate=true)
        {
            // Add buffer before executing animation to prevent spamming
            // Only play animation once the time after visibility changed is greater than or equal to the defined threshold time
            if (m_timerAnimListenStopwatch.IsRunning)
                m_timerAnimListenStopwatch.Stop();

            m_timerAnimListenStopwatch.Reset();
            m_timerAnimListenStopwatch.Start();

            // Set animation flags to determine appropriate course of action
            m_timerAnimTriggerRef.animate = animate;
            m_actionBarAnimTriggerRef.animate = animate;

            // Only animate if desired
            if (animate)
            {
                m_timerAnimTriggerRef.trigger = m_timerVisible ? "Show" : "Hide";
                m_actionBarAnimTriggerRef.trigger = m_timerVisible ? "MoveDown" : "MoveUp";
            }
            else
            {
                // Otherwise, plan to invoke the appropriate animation and jump to last frame (setting speed to 100%)
                m_timerAnimTriggerRef.trigger = m_timerVisible ? "ShowTimerLabel" : "HideTimerLabel";
                m_actionBarAnimTriggerRef.trigger = m_timerVisible ? "MoveActionBarDown" : "MoveActionBarUp";
            }
        }

        /**
         * Determine whether the current auto complete trigger configuration
         * is for single tap or not.
         * 
         * @return bool single tap or not.
         */
        public bool IsSingleTapAutoCompleteTrigger()
        {
            return m_autoCompleteTriggerCode == AUTO_COMPLETE_SINGLE_TAP;
        }

        /**
         * 
         */
        public bool IsTimerVisible()
        {
            return m_timerVisible;
        }

        /**
         * 
         */
        public void CloseGameplaySettings(bool save)
        {
            if (save)
            {
                PlayerPrefs.SetInt(AUTO_COMPLETE_TRIGGER_KEY, m_autoCompleteTriggerCode);
                PlayerPrefs.SetInt(TIMER_VISIBILITY_KEY, m_timerVisible ? 1 : 0);
            }
            else
            {
                // Otherwise, reload the previously saved settings
                LoadSettings();
            }

            gameplayPage.SetActive(false);
            mainSettingsPage.SetActive(true);
        }

        /**
         * 
         */
        public void CloseAudioSettings(bool save)
        {
            if (save)
            {
                // Save to player prefs
                PlayerPrefs.SetFloat(MASTER_VOL_KEY, m_masterVol);
                PlayerPrefs.SetFloat(MUSIC_VOL_KEY, m_musicVol);
                PlayerPrefs.SetFloat(SFX_VOL_KEY, m_sfxVol);
            }
            else
            {
                // Otherwise, reload the previously saved settings
                LoadSettings();
            }

            audioPage.SetActive(false);
            mainSettingsPage.SetActive(true);

        }

        /**
         * 
         */
        public void CloseStatsSettings()
        {
            statsPage.SetActive(false);
            mainSettingsPage.SetActive(true);
        }

        /**
         * 
         */
        public void OnAudioSliderChange(Slider slider)
        {
            SetSliderPercentLabel(slider);

            // Need to normalize to value between 0 and 1
            float newValue = slider.value >= 1.0 ? slider.value / 100.0f : slider.value;
            
            if (slider.CompareTag("MasterVolume"))
            {
                m_masterVol = newValue;
                AudioListener.volume = m_masterVol;

                // If music volume is less than 5% then play the sfx sound
                if (music.volume < 0.05)
                {
                    // Play test sound so the user knows how loud the sound effects are
                    if (!m_loadingSettings && !sfxTestSource.isPlaying)
                    {
                        sfxTestSource.Play();
                    }
                }
            }
            else if (slider.CompareTag("MusicVolume"))
            {
                m_musicVol = newValue;
                music.volume = m_musicVol;
            }
            else if (slider.CompareTag("SFXVolume"))
            {
                m_sfxVol = newValue;

                foreach (AudioSource sfxSource in sfxSources)
                {
                    sfxSource.volume = m_sfxVol;
                }

                // Play test sound so the user knows how loud the sound effects are
                if (!m_loadingSettings && !sfxTestSource.isPlaying)
                {
                    sfxTestSource.Play();
                }
            }
        } 

        /**
         * Handle value changed events from the auto complete trigger dropdown
         * @param TMP_Dropdown dropdown the TextMeshPro Dropdown component controlling
         *                              the autocomplete trigger.
         */
        public void OnAutoCompleteTriggerChanged(TMP_Dropdown dropdown)
        {
            m_autoCompleteTriggerCode = dropdown.value;
            if (GameManager.DEBUG_MODE)
            {
                Debug.Log("Set auto complete trigger to: " + dropdown.itemText +
                    "( " + m_autoCompleteTriggerCode + ")");
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

        /**
         * Invoked by animation event to notify when the timer animation(s)
         * start.
         */
        public void OnTimerAnimationStart()
        {
            if (GameManager.DEBUG_MODE) Debug.Log("Starting timer animation...");
        }

        /**
         * Invoked by animation event to notify when the timer animation(s)
         * complete.
         */
        public void OnTimerAnimationComplete()
        {
            if (GameManager.DEBUG_MODE) Debug.Log("Timer animation complete");
        }
    }
}

