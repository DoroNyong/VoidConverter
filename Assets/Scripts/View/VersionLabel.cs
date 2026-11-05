using UnityEngine;

namespace VoidConverter.View
{
    /// <summary>
    /// A small version label in the bottom-right corner of the screen (the number from Player Settings > Version, which
    /// tools/package_unity.bat sets). Lets a tester say which build they are playing. Creates itself with the first scene.
    /// </summary>
    public class VersionLabel : MonoBehaviour
    {
        static readonly Color LabelColor = new Color(1f, 1f, 1f, 0.45f);
        const float Margin = 0.15f;

        TextMesh label;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (FindFirstObjectByType<VersionLabel>() != null) return;
            new GameObject("Version Label").AddComponent<VersionLabel>();
        }

        void Start()
        {
            label = TextUtil.AddText(transform, "Text", Vector2.zero, "v" + Application.version, 48, 0.03f, LabelColor, 5);
            label.anchor = TextAnchor.LowerRight;
            label.alignment = TextAlignment.Right;
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null || label == null) return;
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            var c = cam.transform.position;
            label.transform.position = new Vector3(c.x + halfWidth - Margin, c.y - halfHeight + Margin, 0f);
        }
    }
}
