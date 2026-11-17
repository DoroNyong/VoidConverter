using UnityEngine;
using UnityEngine.InputSystem;
using VoidConverter.Core;
using VoidConverter.Data;
using VoidConverter.Game;

namespace VoidConverter.View
{
    /// <summary>
    /// The shop screen, drawn full screen during the Shop stage: one card per product (click to buy), the current
    /// gold and HP, and the last purchase message. The Continue button is the shared RoundButtonView.
    /// </summary>
    public class ShopView : MonoBehaviour
    {
        static readonly Color ShadeColor = new Color(0.07f, 0.1f, 0.16f);
        static readonly Color TitleColor = new Color(1f, 0.9f, 0.5f);
        static readonly Color CardColor = new Color(0.2f, 0.3f, 0.42f);
        static readonly Color CardDimColor = new Color(0.16f, 0.18f, 0.22f);
        static readonly Color PriceColor = new Color(1f, 0.9f, 0.4f);
        static readonly Color PriceDimColor = new Color(1f, 0.5f, 0.45f);
        static readonly Color SoldColor = new Color(1f, 1f, 1f, 0.4f);
        // Everything on the shop screen is drawn this much bigger (2026-11-17: roomier while the shop is temporary).
        const float Scale = 1.2f;
        static readonly Vector2 CardSize = new Vector2(1.6f, 2.4f) * Scale;
        const float CardGap = 0.2f * Scale;

        GameController controller;
        GameConfig config;
        Camera cam;
        GameObject root;
        GameObject cardsRoot;
        TextMesh header;
        TextMesh message;

        void Start()
        {
            controller = FindFirstObjectByType<GameController>();
            config = controller.Config;
            cam = Camera.main;

            root = new GameObject("Shop");
            root.transform.SetParent(transform, false);
            // A solid backdrop that always fills the camera, so the board, hand and HUD are hidden behind this screen.
            SpriteUtil.AddSquare(root.transform, "Backdrop", Vector2.zero, 1f, ShadeColor, 60).gameObject.AddComponent<CameraFill>();
            header = TextUtil.AddText(root.transform, "Header", new Vector2(0f, 1.9f * Scale), "", 64, 0.06f * Scale, TitleColor, 70);
            message = TextUtil.AddText(root.transform, "Message", new Vector2(0f, -1.9f * Scale), "", 48, TextUtil.MinTextScale / 48f * Scale, Color.white, 70);
            cardsRoot = new GameObject("Cards");
            cardsRoot.transform.SetParent(root.transform, false);

            controller.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (controller != null) controller.Changed -= Refresh;
        }

        void Update()
        {
            if (controller.Phase != RunPhase.Shop) return;
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

            Vector3 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
            for (int i = 0; i < controller.ShopOffers.Count; i++)
            {
                Vector2 c = CardCenter(i, controller.ShopOffers.Count);
                if (Mathf.Abs(world.x - c.x) <= CardSize.x * 0.5f && Mathf.Abs(world.y - c.y) <= CardSize.y * 0.5f)
                {
                    controller.TryBuy(i);
                    return;
                }
            }
        }

        static Vector2 CardCenter(int index, int count)
        {
            float width = count * CardSize.x + (count - 1) * CardGap;
            return new Vector2(-width * 0.5f + CardSize.x * 0.5f + index * (CardSize.x + CardGap), 0f);
        }

        void Refresh()
        {
            bool open = controller.Phase == RunPhase.Shop;
            root.SetActive(open);
            if (!open) return;

            var p = controller.Player;
            header.text = Loc.T("shop.header", p.Gold, p.Hp, p.MaxHp);
            message.text = controller.Message;

            foreach (Transform child in cardsRoot.transform) Destroy(child.gameObject);

            var offers = controller.ShopOffers;
            for (int i = 0; i < offers.Count; i++)
            {
                var offer = offers[i];
                bool buyable = Shop.CanBuy(p, offer) == Shop.BuyResult.Bought;
                Vector2 c = CardCenter(i, offers.Count);

                SpriteUtil.AddBox(cardsRoot.transform, "Card", c, CardSize.x, CardSize.y, buyable ? CardColor : CardDimColor, 65);

                TextUtil.AddText(cardsRoot.transform, "Name", c + new Vector2(0f, 0.65f) * Scale, offer.Name.Replace(' ', '\n'),
                    48, 0.04f * Scale, offer.Sold ? SoldColor : Color.white, 70);
                TextUtil.AddText(cardsRoot.transform, "Effect", c + new Vector2(0f, -0.1f) * Scale, offer.Summary,
                    48, 0.04f * Scale, offer.Sold ? SoldColor : new Color(0.7f, 1f, 0.75f), 70);

                bool affordable = p.Gold >= offer.Price;
                string priceText = offer.Sold ? Loc.T("shop.sold") : $"{offer.Price} G";
                var priceColor = offer.Sold ? SoldColor : affordable ? PriceColor : PriceDimColor;
                TextUtil.AddText(cardsRoot.transform, "Price", c + new Vector2(0f, -0.8f) * Scale, priceText, 48, 0.045f * Scale, priceColor, 70);
            }
        }
    }
}
