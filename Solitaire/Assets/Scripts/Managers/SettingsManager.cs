﻿using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Solitaire.OrientationManager;
using static Solitaire.Utils;

namespace Solitaire
{
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        private const float PORTRAIT_TIMER_LBL_ANCHORED_POS_X_HIDDEN = -1120.0f;
        private const float PORTRAIT_TIMER_LBL_ANCHORED_POS_X_VISIBLE = 0.0f;

        private const float LANDSCAPE_TIMER_LBL_ANCHORED_POS_X_HIDDEN = -150.0f;
        private const float LANDSCAPE_TIMER_LBL_ANCHORED_POS_X_VISIBLE = 150.0f;
        private const float LANDSCAPE_ACTION_BAR_ANCHORED_POS_Y_HIDDEN = 120.0f;
        private const float LANDSCAPE_ACTION_BAR_ANCHORED_POS_Y_VISIBLE = 0.0f;

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
                // Don't try to animate without a trigger
                if (trigger == null || trigger.Equals(""))
                    return;

                // Don't allow animations if parent game object isn't active
                if (!animator.gameObject.activeInHierarchy)
                    return;

                // Either animate or skip to last keyframe in animation (skipping animation)
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

        [Header("Portrait Settings Pages")]
        public GameObject portraitMainSettingsPage;
        public GameObject portraitWinSettingsPage;
        public GameObject portraitGameplayPage;
        public GameObject portraitAudioPage;
        public GameObject portraitStatsPage;

        [Header("Landscape Settings Pages")]
        public GameObject landscapeMainSettingsPage;
        public GameObject landscapeWinSettingsPage;
        public GameObject landscapeGameplayPage;
        public GameObject landscapeAudioPage;
        public GameObject landscapeStatsPage;

        // Pointers to respective portrait and landscape objects
        private CanvasObjectRef<GameObject> mainSettingsPageRef;
        private CanvasObjectRef<GameObject> winSettingsPageRef;
        private CanvasObjectRef<GameObject> gameplayPageRef;
        private CanvasObjectRef<GameObject> audioPageRef;
        private CanvasObjectRef<GameObject> statsPageRef;

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

        [Header("Portrait Audio Sliders")]
        public Slider portraitSldMasterVol;
        public Slider portraitSldMusicVol;
        public Slider portraitSldSfxVol;

        [Header("Landscape Audio Sliders")]
        public Slider landscapeSldMasterVol;
        public Slider landscapeSldMusicVol;
        public Slider landscapeSldSfxVol;

        // Pointers to respective portrait and landscape objects
        private CanvasObjectRef<Slider> sldMasterVolRef;
        private CanvasObjectRef<Slider> sldMusicVolRef;
        private CanvasObjectRef<Slider> sldSfxVolRef;

        [Header("Portrait Settings Lookup")]
        public List<Button> portraitDrivingButtons = new List<Button>();
        public List<GameObject> portraitCurrPages = new List<GameObject>();
        public List<GameObject> portraitNextPages = new List<GameObject>();

        [Header("Landscape Settings Lookup")]
        public List<Button> landscapeDrivingButtons = new List<Button>();
        public List<GameObject> landscapeCurrPages = new List<GameObject>();
        public List<GameObject> landscapeNextPages = new List<GameObject>();

        // Pointers to respective portrait and landscape objects
        private List<Button> drivingButtons;
        private List<GameObject> currPages;
        private List<GameObject> nextPages;

        [Header("Portrait Animators")]
        public Animator portraitTimerAnimator;
        //public Animator portraitActionBarAnimator;

        [Header("Landscape Animators")]
        public Animator landscapeTimerAnimator;
        public Animator landscapeActionBarAnimator;

        // Pointers to respective portrait and landscape objects
        private Animator timerAnimator;
        private Animator actionBarAnimator;

        [Header("Portrait Miscellaneous")]
        public GameObject portraitLblHighScoreNotification;
        public TMP_Dropdown portraitDpnAutoCompleteTrigger;
        public Slider portraitSldTimerLabel;
        public ScrollRect portraitStatsScrollRect;

        [Header("Landscape Miscellaneous")]
        public GameObject landscapeLblHighScoreNotification;
        public TMP_Dropdown landscapeDpnAutoCompleteTrigger;
        public Slider landscapeSldTimerLabel;
        public ScrollRect landscapeStatsScrollRect;

        // Pointers to respective portrait and landscape objects
        private CanvasObjectRef<GameObject> lblHighScoreNotificationRef;
        private CanvasObjectRef<TMP_Dropdown> dpnAutoCompleteTriggerRef;
        private CanvasObjectRef<Slider> sldTimerLabelRef;
        private CanvasObjectRef<ScrollRect> statsScrollRectRef;

        private Dictionary<Button, SettingsPage> m_settingsPagesLookup;

        private Slider[] m_settingsSliders;

        private AnimatorTriggerRef m_landscapeTimerAnimTriggerRef;
        private AnimatorTriggerRef m_portraitTimerAnimTriggerRef;
        AnimatorTriggerRef m_timerAnimTriggerRef;
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

            // Treat music audio source as singleton
            music = GameObject.FindGameObjectWithTag("Music").GetComponent<AudioSource>();

            mainSettingsPageRef = new CanvasObjectRef<GameObject>();
            winSettingsPageRef = new CanvasObjectRef<GameObject>();
            gameplayPageRef = new CanvasObjectRef<GameObject>();
            audioPageRef = new CanvasObjectRef<GameObject>();
            statsPageRef = new CanvasObjectRef<GameObject>();

            sldMasterVolRef = new CanvasObjectRef<Slider>();
            sldMusicVolRef = new CanvasObjectRef<Slider>();
            sldSfxVolRef = new CanvasObjectRef<Slider>();

            lblHighScoreNotificationRef = new CanvasObjectRef<GameObject>();
            dpnAutoCompleteTriggerRef = new CanvasObjectRef<TMP_Dropdown>();
            sldTimerLabelRef = new CanvasObjectRef<Slider>();
            statsScrollRectRef = new CanvasObjectRef<ScrollRect>();

            SetTargetCanvasObjectsByOrientation(OrientationManager.GetCurrentOrientation());

            // Initialize timer and action bar animation trigger references
            m_landscapeTimerAnimTriggerRef = new AnimatorTriggerRef().Init(landscapeTimerAnimator);
            m_portraitTimerAnimTriggerRef = new AnimatorTriggerRef().Init(portraitTimerAnimator);

            // Set action bar animator permanently to landscape action bar animator
            actionBarAnimator = landscapeActionBarAnimator;

            m_actionBarAnimTriggerRef = new AnimatorTriggerRef().Init(actionBarAnimator);

            // Load saved settings if they exist and initialize configurations
            LoadSettings();

            InitizlizeConfiguration();
        }

        /**
         * Initialize all references for canvas objects, assuming the pointers are not null.
         */
        private void InitizlizeConfiguration()
        {
            m_settingsSliders = new Slider[]
            {
                sldMasterVolRef.current,
                sldMusicVolRef.current,
                sldSfxVolRef.current
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
            AudioListener.volume = sldMasterVolRef.current.value / 100.0f;
            music.volume = sldMusicVolRef.current.value / 100.0f;
            foreach (AudioSource sfxSource in sfxSources)
            {
                sfxSource.volume = sldSfxVolRef.current.value / 100.0f;
            }

            // Keep portrait and landscape sliders in sync with each other
            // Audio Sliders
            sldMasterVolRef.DoAction(() =>
            {
                if (sldMasterVolRef.previous != null)
                {
                    sldMasterVolRef.previous.SetValueWithoutNotify(sldMasterVolRef.current.value);
                }
            });

            sldMusicVolRef.DoAction(() =>
            {
                if (sldMusicVolRef.previous != null)
                {
                    sldMusicVolRef.previous.SetValueWithoutNotify(sldMusicVolRef.current.value);
                }
            });

            sldSfxVolRef.DoAction(() =>
            {
                if (sldSfxVolRef.previous != null)
                {
                    sldSfxVolRef.previous.SetValueWithoutNotify(sldSfxVolRef.current.value);
                }
            });

            // Settings pages
            mainSettingsPageRef.DoAction(() => {
                if (mainSettingsPageRef.previous != null)
                {
                    mainSettingsPageRef.previous.SetActive(mainSettingsPageRef.current.activeInHierarchy);
                }
            });

            winSettingsPageRef.DoAction(() => {
                if (winSettingsPageRef.previous != null)
                {
                    winSettingsPageRef.previous.SetActive(winSettingsPageRef.current.activeInHierarchy);
                }
            });

            gameplayPageRef.DoAction(() => {
                if (gameplayPageRef.previous != null)
                {
                    gameplayPageRef.previous.SetActive(gameplayPageRef.current.activeInHierarchy);
                }
            });

            audioPageRef.DoAction(() => {
                if (audioPageRef.previous != null)
                {
                    audioPageRef.previous.SetActive(audioPageRef.current.activeInHierarchy);
                }
            });

            statsPageRef.DoAction(() => {
                if (statsPageRef.previous != null)
                {
                    statsPageRef.previous.SetActive(statsPageRef.current.activeInHierarchy);
                }
            });

            // Miscellaneous
            lblHighScoreNotificationRef.DoAction(() =>
            {
                if (lblHighScoreNotificationRef.previous != null)
                {
                    lblHighScoreNotificationRef.previous.SetActive(lblHighScoreNotificationRef.current.activeInHierarchy);
                }
            });

            dpnAutoCompleteTriggerRef.DoAction(() =>
            {
                if (dpnAutoCompleteTriggerRef.previous != null)
                {
                    dpnAutoCompleteTriggerRef.previous.SetValueWithoutNotify(dpnAutoCompleteTriggerRef.current.value);
                }
            });

            sldTimerLabelRef.DoAction(() =>
            {
                if (sldTimerLabelRef.previous != null)
                {
                    sldTimerLabelRef.previous.SetValueWithoutNotify(sldTimerLabelRef.current.value);
                }
            });
        }

        /**
         * Marshal the target canvas objects based on the specified orientation.
         * The size and position of the canvas objects vary based on orientation. Thus,
         * it's necessary to swap the set of canvas objects to use based on the current
         * orientation. This function serves that purpose.
         * 
         * @param orientation the screen orientation used to determine the set of target
         *                    canvas objects to use.
         */
        public void SetTargetCanvasObjectsByOrientation(Orientations orientation)
        {
            bool portraitOrientation = orientation.Equals(Orientations.PORTRAIT);

            // Settings Pages
            mainSettingsPageRef.Set(portraitMainSettingsPage, landscapeMainSettingsPage, portraitOrientation);
            winSettingsPageRef.Set(portraitWinSettingsPage, landscapeWinSettingsPage, portraitOrientation);
            gameplayPageRef.Set(portraitGameplayPage, landscapeGameplayPage, portraitOrientation);
            audioPageRef.Set(portraitAudioPage, landscapeAudioPage, portraitOrientation);
            statsPageRef.Set(portraitStatsPage, landscapeStatsPage, portraitOrientation);

            // Audio Sliders
            sldMasterVolRef.Set(portraitSldMasterVol, landscapeSldMasterVol, portraitOrientation);
            sldMusicVolRef.Set(portraitSldMusicVol, landscapeSldMusicVol, portraitOrientation);
            sldSfxVolRef.Set(portraitSldSfxVol, landscapeSldSfxVol, portraitOrientation);

            // Settings Lookup
            drivingButtons = portraitOrientation ? portraitDrivingButtons : landscapeDrivingButtons;
            currPages = portraitOrientation ? portraitCurrPages : landscapeCurrPages;
            nextPages = portraitOrientation ? portraitNextPages : landscapeNextPages;

            // Animators
            timerAnimator = portraitOrientation ? portraitTimerAnimator : landscapeTimerAnimator;
            m_timerAnimTriggerRef = portraitOrientation ? m_portraitTimerAnimTriggerRef : m_landscapeTimerAnimTriggerRef;

            // Miscellaneous
            lblHighScoreNotificationRef.Set(portraitLblHighScoreNotification, landscapeLblHighScoreNotification, portraitOrientation);
            dpnAutoCompleteTriggerRef.Set(portraitDpnAutoCompleteTrigger, landscapeDpnAutoCompleteTrigger, portraitOrientation);
            sldTimerLabelRef.Set(portraitSldTimerLabel, landscapeSldTimerLabel, portraitOrientation);

            statsScrollRectRef.DoAction(() =>
            {
                if (statsScrollRectRef.previous != null)
                {
                    // Keep the vertical scroll position in sync between the screen orientations
                    float currVertPos = statsScrollRectRef.current.verticalNormalizedPosition;
                    statsScrollRectRef.previous.verticalNormalizedPosition = currVertPos;
                }
            });

            statsScrollRectRef.Set(portraitStatsScrollRect, landscapeStatsScrollRect, portraitOrientation);

            // Load saved settings if they exist
            //LoadSettings();

            HandleTimerVisibility(false);

            // Need to reinitialize based on new values
            InitizlizeConfiguration();
        }

        public GameObject GetWinSettingsPage()
        {
            return winSettingsPageRef.current;
        }

        public GameObject GetHighScoreLabel()
        {
            return lblHighScoreNotificationRef.current;
        }

        /**
         * Load previously saves settings from player prefs. If any settings
         * do not exist from player prefs for the slider values then the default
         * value is 1.0f.
         */
        public void LoadSettings()
        {
            m_loadingSettings = true;
            sldMasterVolRef.current.value = PlayerPrefs.GetFloat(MASTER_VOL_KEY, 1.0f) * 100.0f;
            OnAudioSliderChange(sldMasterVolRef.current);

            sldMusicVolRef.current.value = PlayerPrefs.GetFloat(MUSIC_VOL_KEY, 1.0f) * 100.0f;
            OnAudioSliderChange(sldMusicVolRef.current);

            sldSfxVolRef.current.value = PlayerPrefs.GetFloat(SFX_VOL_KEY, 1.0f) * 100.0f;
            OnAudioSliderChange(sldSfxVolRef.current);

            // Double tap is the default (1)
            m_autoCompleteTriggerCode = PlayerPrefs.GetInt(AUTO_COMPLETE_TRIGGER_KEY, 1);
            dpnAutoCompleteTriggerRef.current.SetValueWithoutNotify(m_autoCompleteTriggerCode);

            m_timerVisible = PlayerPrefs.GetInt(TIMER_VISIBILITY_KEY, 1) == 1;
            sldTimerLabelRef.current.SetValueWithoutNotify(m_timerVisible ? 1 : 0);

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

            bool portraitOrientation = OrientationManager.IsPortraitOrientation();

            // Only animate if desired
            if (animate)
            {
                m_timerAnimTriggerRef.trigger = m_timerVisible ? "Show" : "Hide";
                m_actionBarAnimTriggerRef.trigger = m_timerVisible ? "MoveDown" : "MoveUp";
            }
            else
            {
                // Otherwise, plan to invoke the appropriate animation and jump to last frame (setting speed to 100%)
                string showTrigger = portraitOrientation ? "ShowTimerLabelPortrait" : "ShowTimerLabelLandscape";
                string hideTrigger = portraitOrientation ? "HideTimerLabelPortrait" : "HideTimerLabelLandscape";

                m_timerAnimTriggerRef.trigger = m_timerVisible ? showTrigger : hideTrigger;
                m_actionBarAnimTriggerRef.trigger = m_timerVisible ? "MoveActionBarDown" : "MoveActionBarUp";
            }

            // Determine which of the timer labels is not currently active based on screen orientation and set its
            // anchored position accordingly to keep it in sync with the currently active/visible timer label
            RectTransform oppositeTimerRect;
            float newAnchoredX;
            if (portraitOrientation)
            {
                oppositeTimerRect = landscapeTimerAnimator.GetComponent<RectTransform>();
                newAnchoredX = m_timerVisible ? LANDSCAPE_TIMER_LBL_ANCHORED_POS_X_VISIBLE : LANDSCAPE_TIMER_LBL_ANCHORED_POS_X_HIDDEN;

                // Need to move the action buttons as well
                float newAnchoredY = m_timerVisible ? LANDSCAPE_ACTION_BAR_ANCHORED_POS_Y_VISIBLE : LANDSCAPE_ACTION_BAR_ANCHORED_POS_Y_HIDDEN;
                RectTransform actionBarRect = actionBarAnimator.GetComponent<RectTransform>();
                actionBarRect.anchoredPosition = new Vector2(actionBarRect.anchoredPosition.x, newAnchoredY);
            }
            else
            {
                oppositeTimerRect = portraitTimerAnimator.GetComponent<RectTransform>();
                newAnchoredX = m_timerVisible ? PORTRAIT_TIMER_LBL_ANCHORED_POS_X_VISIBLE : PORTRAIT_TIMER_LBL_ANCHORED_POS_X_HIDDEN;
            }

            // Set the anchored position for the timer label that isn't currently active/visible
            oppositeTimerRect.anchoredPosition = new Vector2(newAnchoredX, oppositeTimerRect.anchoredPosition.y);
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

            gameplayPageRef.DoAction(() =>
            {
                gameplayPageRef.current.SetActive(false);
                gameplayPageRef.previous.SetActive(false);
            });

            mainSettingsPageRef.DoAction(() =>
            {
                mainSettingsPageRef.current.SetActive(true);
                mainSettingsPageRef.previous.SetActive(true);
            });
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

            audioPageRef.current.SetActive(false);
            mainSettingsPageRef.current.SetActive(true);
        }

        /**
         * 
         */
        public void CloseStatsSettings()
        {
            statsPageRef.current.SetActive(false);
            mainSettingsPageRef.current.SetActive(true);
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

