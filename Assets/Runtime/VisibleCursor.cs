using UnityEngine;

public class VisibleCursor : MonoBehaviour
{
    private void OnGUI() {
        var mousePosition = Input.mousePosition;

        float x = mousePosition.x;
        float y = Screen.height - mousePosition.y;
        float width = 200;
        float height = 200;
        var rect = new Rect(x, y, width, height);

        GUI.Label(rect, "<Cursor");
    }
}
