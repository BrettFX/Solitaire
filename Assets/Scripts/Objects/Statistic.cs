using TMPro;
using UnityEngine;

namespace Solitaire
{
    public class Statistic : MonoBehaviour
    {
        public TextMeshProUGUI statLabel;
        public TextMeshProUGUI statValue;

        public void SetText(string text)
        {
            statValue.text = text;
        }
    }
}

