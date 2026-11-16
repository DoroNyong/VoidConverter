using VoidConverter.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using VoidConverter.Data;
using VoidConverter.Game;

namespace VoidConverter.View
{
    /// <summary>
    /// The boss marker in the free corner above the board's right side during the boss stage. It shows how many blocks were placed out of the
    /// number needed; once the boss is open it lights up and a click defeats it.
    /// </summary>
    public class BossView : MonoBehaviour
    {
        static readonly Color WaitingColor = new Color(0.3f, 0.2f, 0.25f);
        static readonly Color ReadyColor = new Color(0.85f, 0.25f, 0.25f);

        GameController controller;
        GameConfig config;
        Camera cam;
        GameObject root;
        SpriteRenderer body;
        TextMesh title;
        TextMesh status;
        Vector2 center;
        Rect area;

        void Start()
        {
            controller = FindFirstObjectByType<GameController>();
            config = controller.Config;
            cam = Camera.main;

            // The free corner above the board's right side (HudLayout.Boss).
            area = HudLayout.Boss;
            center = area.center;

            root = new GameObject("Boss Marker");
            root.transform.SetParent(transform, false);
            body = HudLayout.AddPanel(root.transform, "Body", area, WaitingColor, 5);
            title = TextUtil.AddText(root.transform, "Title", center + new Vector2(0f, 0.45f), Loc.T("boss.title"), 64, 0.07f, Color.white, 10);
            status = TextUtil.AddText(root.transform, "Status", center + new Vector2(0f, -0.4f), "", 44, 0.045f, Color.white, 10);

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

            Vector2 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
            if (area.Contains(world)) controller.DefeatBoss();
        }

        void Refresh()
        {
            bool boss = controller.IsBossStage;
            root.SetActive(boss);
            if (!boss) return;

            bool ready = controller.BossReady;
            body.color = ready ? ReadyColor : WaitingColor;
            title.text = Loc.T("boss.title");
            status.text = ready
                ? Loc.T(controller.IsFinalBossRound ? "boss.clickDefeat" : "boss.clickNext")
                : controller.PickOneRule
                    ? (controller.BossRounds > 1
                        ? Loc.T("boss.phase", controller.BossRound, controller.BossRounds, controller.PlacementsUsed, controller.MinPlacements)
                        : Loc.T("boss.place", controller.PlacementsUsed, controller.MinPlacements))
                : controller.ClearRule
                    ? $"Monsters\nleft {controller.MonstersLeft}"
                    : $"Hold out\n{Mathf.Min(controller.PlacementsUsed, controller.BossHoldTarget)}/{controller.BossHoldTarget}";
        }
    }
}
