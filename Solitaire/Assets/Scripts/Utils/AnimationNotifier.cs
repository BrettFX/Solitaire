using UnityEngine;

namespace Solitaire
{
    public class AnimationNotifier : MonoBehaviour
    {
        public void NotifySettingsTimerAnimStart()
        {
            SettingsManager.Instance.OnTimerAnimationStart();
        }

        public void NotifySettingsTimerAnimComplete()
        {
            SettingsManager.Instance.OnTimerAnimationComplete();
        }
    }
}
