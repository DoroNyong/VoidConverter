using UnityEngine;
using UnityEngine.InputSystem;
using VoidConverter.Data;
using VoidConverter.Game;

namespace VoidConverter.View
{
    /// <summary>
    /// The boss marker to the right of the grid during the boss stage. It shows how many blocks were placed out of the
    /// number needed; once the boss is open it lights up and a click defeats it.
    /// </summary>
    public class BossView : MonoBehaviour
    {
        static readonly Color WaitingColor = new Color(0.3f, 0.2f, 0.25f);
        static readonly Color ReadyColor = new Color(0.85f, 0.25f, 0.25f);
        static readonly Vector2 Size = new Vector2(2.2f, 2.2f);

        GameController controller;
        GameConfig config;
        Camera cam;
        GameObject root;
        SpriteRenderer body;
        TextMesh title;
        TextMesh status;
        Vector2 center;

        void Start()
        {
            controller = FindFirstObjectByType<GameController>();
            config = controller.Config;
            cam = Camera.main;

            float step = config.cellSize + config.cellGap;
            // Just right of the grid, level with its middle.
            center = new Vector2(config.gridWidth * step * 0.5f + 0.4f + Size.x * 0.5f, 0f);

            root = new GameObject("Boss Marker");
            root.transform.SetParent(transform, false);
            body = SpriteUtil.AddSquare(root.transform, "Body", center, 1f, WaitingColor, 5);
            body.transform.localScale = new Vector3(Size.x, Size.y, 1f);
            title = TextUtil.AddText(root.transform, "Title", center + new Vector2(0f, 0.5f), "BOSS", 64, 0.08f, Color.white, 10);
            status = TextUtil.AddText(root.transform, "Status", center + new Vector2(0f, -0.5f), "", 48, 0.04f, Color.white, 10);

            controller.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (controller != null) controller.Changed -= Refresh;
        }

        void Update()
        {
            if (!controller.BossReady) return;
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

            Vector3 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
            bool inside = Mathf.Abs(world.x - center.x) <= Size.x * 0.5f && Mathf.Abs(world.y - center.y) <= Size.y * 0.5f;
            if (inside) controller.DefeatBoss();
        }

        void Refresh()
        {
            bool boss = controller.IsBossStage;
            root.SetActive(boss);
            if (!boss) return;

            bool ready = controller.BossReady;
            body.color = ready ? ReadyColor : WaitingColor;
            status.text = ready
                ? "Click to\ndefeat!"
                : $"Hold out\n{Mathf.Min(controller.PlacementsUsed, controller.BossHoldTarget)}/{controller.BossHoldTarget}";
        }
    }
}
