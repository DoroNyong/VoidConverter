using UnityEngine;

namespace VoidConverter.View
{
    /// <summary>Keeps a 1x1 square sprite exactly covering the main camera's view, also after the window is resized.</summary>
    public class CameraFill : MonoBehaviour
    {
        /// <summary>The camera to cover; the main camera when not set.</summary>
        public Camera Target;

        void LateUpdate()
        {
            var cam = Target != null ? Target : Camera.main;
            if (cam == null) return;

            float height = cam.orthographicSize * 2f;
            transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
            // A little larger than the view, so no edge pixel shows through.
            transform.localScale = new Vector3(height * cam.aspect + 0.2f, height + 0.2f, 1f);
        }
    }
}
