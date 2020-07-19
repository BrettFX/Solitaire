using UnityEngine;

namespace Solitaire
{
    public class ObjectDragger : MonoBehaviour
    {
        private Vector3 screenPoint;
        private Vector3 offset;

        void OnMouseDown()
        {
            if (GameManager.DEBUG_MODE) { Debug.Log("Clicked on " + gameObject.name); }

            screenPoint = Camera.main.WorldToScreenPoint(gameObject.transform.position);
            Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
            offset = gameObject.transform.position - Camera.main.ScreenToWorldPoint(curScreenPoint);

        }

        void OnMouseDrag()
        {
            // Only allow dragging cards
            if (gameObject.tag.Equals("Card"))
            {
                Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
                Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint) + offset;
                transform.position = curPosition;
            }
        }
    }
}
