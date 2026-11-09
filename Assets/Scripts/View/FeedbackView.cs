using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using VoidConverter.Data;
using VoidConverter.Game;

namespace VoidConverter.View
{
    /// <summary>
    /// The feel of the core moment: when a block is placed and one cell fires, it flashes, bursts, shows what it
    /// paid out and plays a sound. A won fight hits harder (slash or magic rings, shockwave, a short freeze, a camera
    /// punch), more so against a stronger monster. Damage shakes the camera and flashes the screen red, a heal flashes
    /// it green, softly. M mutes the sound. Creates itself when a scene with a GameController loads, so no scene edit
    /// is needed.
    /// </summary>
    public class FeedbackView : MonoBehaviour
    {
        static readonly Color GoldTextColor = new Color(1f, 0.9f, 0.3f);
        static readonly Color HealTextColor = new Color(0.5f, 1f, 0.55f);
        static readonly Color DamageTextColor = new Color(1f, 0.4f, 0.35f);
        static readonly Color MagicColor = new Color(0.45f, 0.65f, 1f);
        static readonly Color DamageFlashColor = new Color(0.9f, 0.1f, 0.1f);
        static readonly Color HealFlashColor = new Color(0.3f, 0.95f, 0.4f);

        const float SoundVolume = 0.6f;
        const int EffectOrder = 50;
        // Above every other view except the screen fader (1000).
        const int ScreenFlashOrder = 900;
        // A monster of this power (and weaker / stronger) gets the weakest (strongest) win effect.
        const int WeakPower = 1;
        const int StrongPower = 5;

        GameController controller;
        GridView gridView;
        // The camera the camera effects (shake, punch, screen flash) act on, and the size of one cell.
        Camera cam;
        float cellSize = 1f;
        // Width / height of a tile. Above 1 the board is a flattened diamond, and the effects follow its shape
        // (a diamond pop, flattened rings and bursts). 1 keeps the plain square look of the comparison scene.
        float tileAspect = 1f;
        bool Diamond => tileAspect > 1.001f;
        // True when driven by code (the effect comparison scene) instead of a GameController.
        bool standalone;
        AudioSource source;
        AudioClip winClip, winMagicClip, goldClip, healClip, loseClip, trapClip;
        SpriteRenderer screenFlash;
        Coroutine flashing, shaking, punching;
        Vector3 shakeBase;
        float punchBaseSize;
        // While unscaled time is below this, every effect holds still (the "hit stop" freeze).
        float holdUntil;
        bool muted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (FindFirstObjectByType<GameController>() == null) return;
            if (FindFirstObjectByType<FeedbackView>() != null) return;
            new GameObject("Feedback").AddComponent<FeedbackView>();
        }

        void Start()
        {
            if (standalone) return;
            controller = FindFirstObjectByType<GameController>();
            gridView = FindFirstObjectByType<GridView>();
            if (controller == null || gridView == null)
            {
                enabled = false;
                return;
            }

            // The sounds need a listener; the default camera normally has one.
            if (FindFirstObjectByType<AudioListener>() == null && Camera.main != null) Camera.main.gameObject.AddComponent<AudioListener>();
            Init(Camera.main, controller.Config.cellSize);
            tileAspect = controller.Config.tileAspect;
            controller.Fired += OnFired;
        }

        /// <summary>
        /// Drives the effects from code instead of a GameController: they act on <paramref name="camera"/> and are
        /// played with <see cref="Play"/>. Used by the effect comparison scene, which supplies its own listener.
        /// </summary>
        public void Setup(Camera camera, float cell)
        {
            standalone = true;
            Init(camera, cell);
        }

        void Init(Camera camera, float cell)
        {
            cam = camera;
            cellSize = cell;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;

            winClip = SfxSynth.Win(false);
            winMagicClip = SfxSynth.Win(true);
            goldClip = SfxSynth.Gold();
            healClip = SfxSynth.Heal();
            loseClip = SfxSynth.Lose();
            trapClip = SfxSynth.Trap();

            screenFlash = SpriteUtil.AddSquare(transform, "Screen Flash", Vector2.zero, 1f, new Color(0.9f, 0.1f, 0.1f, 0f), ScreenFlashOrder);
            var fill = screenFlash.gameObject.AddComponent<CameraFill>();
            fill.Target = cam;
            screenFlash.enabled = false;
        }

        void OnDestroy()
        {
            if (controller != null) controller.Fired -= OnFired;
        }

        void Update()
        {
            if (standalone) return;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.mKey.wasPressedThisFrame)
            {
                muted = !muted;
                AudioListener.volume = muted ? 0f : 1f;
            }
        }

        // The frame time effects advance by: zero during a hit stop.
        float Dt => Time.unscaledTime < holdUntil ? 0f : Time.unscaledDeltaTime;

        void OnFired(FeedbackEvent e)
        {
            Play(e, gridView.CellToWorld(e.X, e.Y));
        }

        /// <summary>Plays the effects for one fired cell at a world position.</summary>
        public void Play(FeedbackEvent e, Vector2 pos)
        {
            Color color;
            string text;
            AudioClip clip;
            bool damage = false;
            float burstSize = 1f;
            float volume = SoundVolume;

            switch (e.Kind)
            {
                case FeedbackKind.Win:
                    color = GoldTextColor;
                    text = "+$" + e.Amount;
                    clip = e.Magic ? winMagicClip : winClip;
                    // 0 (weak monster) .. 1 (strong monster).
                    float power = Mathf.InverseLerp(WeakPower, StrongPower, e.Power);
                    burstSize = Mathf.Lerp(1.5f, 2.2f, power);
                    volume = Mathf.Lerp(0.75f, 1f, power);
                    HitWin(pos, e.Magic, power);
                    break;
                case FeedbackKind.Gold:
                    color = GoldTextColor;
                    text = "+$" + e.Amount;
                    clip = goldClip;
                    break;
                case FeedbackKind.Heal:
                    color = HealTextColor;
                    text = e.Amount > 0 ? "+" + e.Amount + " HP" : "HP full";
                    clip = healClip;
                    // A soft green flash over the whole screen.
                    if (e.Amount > 0) StartFlash(HealFlashColor, 0.16f, 0.5f);
                    break;
                case FeedbackKind.Trap:
                    color = DamageTextColor;
                    text = "-" + e.Amount + " HP";
                    clip = trapClip;
                    damage = true;
                    break;
                default:
                    color = DamageTextColor;
                    text = "-" + e.Amount + " HP";
                    clip = loseClip;
                    damage = true;
                    break;
            }

            if (clip != null)
            {
                // A little pitch variation so repeated hits do not sound identical.
                source.pitch = Random.Range(0.94f, 1.06f);
                source.PlayOneShot(clip, volume);
            }
            StartCoroutine(Pop(pos));
            StartCoroutine(Burst(pos, color, burstSize));
            StartCoroutine(FloatText(pos, text, color, e.Kind == FeedbackKind.Win ? 1.35f : 1f));
            if (damage)
            {
                StartShake(0.14f, 0.28f);
                StartFlash(DamageFlashColor, 0.35f, 0.35f);
            }
        }

        // The extra impact of a won fight: a freeze, a slash (physical) or rings (magic), a shockwave and a camera punch.
        void HitWin(Vector2 pos, bool magic, float power)
        {
            // Freeze every effect for a moment on the impact frame (0.05s .. 0.09s).
            holdUntil = Time.unscaledTime + Mathf.Lerp(0.05f, 0.09f, power);
            if (magic)
            {
                StartCoroutine(Ring(pos, MagicColor, 0.6f, 3.2f + power, 0.45f, 0.9f));
                StartCoroutine(Ring(pos, Color.white, 0.4f, 2.2f + power, 0.35f, 0.7f));
            }
            else
            {
                StartCoroutine(Slash(pos, 1.3f + power * 0.5f));
                StartCoroutine(Ring(pos, Color.white, 0.5f, 3.0f + power, 0.4f, 0.8f));
            }
            StartPunch(Mathf.Lerp(0.035f, 0.07f, power), 0.22f);
            StartShake(Mathf.Lerp(0.05f, 0.1f, power), 0.2f);
        }

        // A white flash on the fired cell that grows and fades.
        IEnumerator Pop(Vector2 pos)
        {
            var sr = Diamond
                ? SpriteUtil.AddDiamond(transform, "Pop", pos, cellSize * tileAspect, cellSize, new Color(1f, 1f, 1f, 0.85f), EffectOrder)
                : SpriteUtil.AddSquare(transform, "Pop", pos, cellSize, new Color(1f, 1f, 1f, 0.85f), EffectOrder);
            const float duration = 0.3f;
            for (float t = 0f; t < duration; t += Dt)
            {
                float k = t / duration;
                sr.transform.localScale = new Vector3(Diamond ? tileAspect : 1f, 1f, 1f) * cellSize * Mathf.Lerp(1f, 1.5f, k);
                sr.color = new Color(1f, 1f, 1f, 0.85f * (1f - k));
                yield return null;
            }
            Destroy(sr.gameObject);
        }

        // A bright diagonal line that sweeps through the cell, then fades.
        IEnumerator Slash(Vector2 pos, float length)
        {
            var sr = SpriteUtil.AddSquare(transform, "Slash", pos, 1f, Color.white, EffectOrder + 3);
            sr.transform.rotation = Quaternion.Euler(0f, 0f, 40f);
            const float sweep = 0.07f;
            const float fade = 0.18f;
            for (float t = 0f; t < sweep + fade; t += Dt)
            {
                float width = Mathf.Lerp(0.16f, 0.04f, Mathf.Clamp01((t - sweep) / fade));
                float reach = Mathf.Clamp01(t / sweep);
                sr.transform.localScale = new Vector3(length * reach, width, 1f);
                float alpha = t < sweep ? 1f : 1f - (t - sweep) / fade;
                sr.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }
            Destroy(sr.gameObject);
        }

        // A ring that grows out of the cell and fades (the shockwave, or a magic circle).
        IEnumerator Ring(Vector2 pos, Color color, float fromSize, float toSize, float duration, float startAlpha)
        {
            var sr = SpriteUtil.AddRing(transform, "Ring", pos, fromSize, color, EffectOrder + 2);
            for (float t = 0f; t < duration; t += Dt)
            {
                float k = t / duration;
                // Fast at first, then slowing down.
                float eased = 1f - (1f - k) * (1f - k);
                // On the flattened board a ring lies on the ground: an ellipse as flat as a tile.
                sr.transform.localScale = new Vector3(1f, Diamond ? 1f / tileAspect : 1f, 1f) * Mathf.Lerp(fromSize, toSize, eased);
                sr.color = new Color(color.r, color.g, color.b, startAlpha * (1f - k));
                yield return null;
            }
            Destroy(sr.gameObject);
        }

        // Small squares flying out from the cell in the outcome's color.
        IEnumerator Burst(Vector2 pos, Color color, float size)
        {
            const int count = 12;
            const float duration = 0.5f;
            var bits = new SpriteRenderer[count];
            var velocity = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                bits[i] = SpriteUtil.AddSquare(transform, "Burst", pos, 0.12f * size, color, EffectOrder + 1);
                float angle = (i + Random.value * 0.6f) / count * Mathf.PI * 2f;
                velocity[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(1.8f, 3.4f) * size;
            }
            for (float t = 0f; t < duration; t += Dt)
            {
                float k = t / duration;
                for (int i = 0; i < count; i++)
                {
                    bits[i].transform.localPosition = pos + velocity[i] * t * (1f - k * 0.5f);
                    bits[i].color = new Color(color.r, color.g, color.b, 1f - k);
                }
                yield return null;
            }
            for (int i = 0; i < count; i++) Destroy(bits[i].gameObject);
        }

        // The payout text pops in large, settles, rises from the cell and fades out.
        IEnumerator FloatText(Vector2 pos, string text, Color color, float scale)
        {
            var tm = TextUtil.AddText(transform, "Float Text", pos, text, 64, 0.1f, color, EffectOrder + 5);
            const float duration = 1.0f;
            const float rise = 0.9f;
            for (float t = 0f; t < duration; t += Dt)
            {
                float k = t / duration;
                tm.transform.localPosition = pos + new Vector2(0f, rise * (1f - (1f - k) * (1f - k)));
                // Starts 1.6x too big and settles to its size within the first 0.12 seconds.
                float pop = Mathf.Lerp(1.6f, 1f, Mathf.Clamp01(t / 0.12f));
                tm.transform.localScale = Vector3.one * pop * scale;
                float alpha = k < 0.5f ? 1f : 1f - (k - 0.5f) * 2f;
                tm.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }
            Destroy(tm.gameObject);
        }

        void StartShake(float strength, float duration)
        {
            if (cam == null) return;
            if (shaking != null)
            {
                StopCoroutine(shaking);
                cam.transform.position = shakeBase;
            }
            shakeBase = cam.transform.position;
            shaking = StartCoroutine(Shake(cam, strength, duration));
        }

        IEnumerator Shake(Camera cam, float strength, float duration)
        {
            for (float t = 0f; t < duration; t += Dt)
            {
                float fade = 1f - t / duration;
                cam.transform.position = shakeBase + (Vector3)(Random.insideUnitCircle * strength * fade);
                yield return null;
            }
            cam.transform.position = shakeBase;
            shaking = null;
        }

        // The view zooms in a little on impact and eases back out.
        void StartPunch(float amount, float duration)
        {
            if (cam == null) return;
            if (punching != null)
            {
                StopCoroutine(punching);
                cam.orthographicSize = punchBaseSize;
            }
            punchBaseSize = cam.orthographicSize;
            punching = StartCoroutine(Punch(cam, amount, duration));
        }

        IEnumerator Punch(Camera cam, float amount, float duration)
        {
            for (float t = 0f; t < duration; t += Dt)
            {
                float k = t / duration;
                // Zooms in at once, then eases back.
                cam.orthographicSize = punchBaseSize * (1f - amount * (1f - k) * (1f - k));
                yield return null;
            }
            cam.orthographicSize = punchBaseSize;
            punching = null;
        }

        void StartFlash(Color color, float alpha, float duration)
        {
            if (flashing != null) StopCoroutine(flashing);
            flashing = StartCoroutine(Flash(color, alpha, duration));
        }

        IEnumerator Flash(Color color, float alpha, float duration)
        {
            screenFlash.enabled = true;
            for (float t = 0f; t < duration; t += Dt)
            {
                screenFlash.color = new Color(color.r, color.g, color.b, alpha * (1f - t / duration));
                yield return null;
            }
            screenFlash.enabled = false;
            flashing = null;
        }
    }
}
