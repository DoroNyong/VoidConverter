using System;
using System.Collections;
using UnityEngine;
using VoidConverter.Data;
using VoidConverter.Game;

namespace VoidConverter.View
{
    /// <summary>
    /// Screen transitions: fades the whole view to black, lets the game switch screens while it is hidden, then fades
    /// back in. Registers itself as the controller's Transitioner, so every stage change and restart goes through it.
    /// Later the title screen can use the same Run method when switching scenes.
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        // Above every other view (the highest of those is 100).
        const int Order = 1000;

        GameController controller;
        GameConfig config;
        SpriteRenderer cover;
        Coroutine running;

        void Start()
        {
            controller = FindFirstObjectByType<GameController>();
            config = controller.Config;

            cover = SpriteUtil.AddSquare(transform, "Cover", Vector2.zero, 1f, new Color(0f, 0f, 0f, 0f), Order);
            cover.gameObject.AddComponent<CameraFill>();
            cover.enabled = false;

            controller.Transitioner = Run;
        }

        void OnDestroy()
        {
            if (controller != null && controller.Transitioner == Run) controller.Transitioner = null;
        }

        /// <summary>Waits <paramref name="delay"/>, fades out, calls <paramref name="change"/>, fades in, then calls <paramref name="finished"/>.</summary>
        public void Run(Action change, float delay, Action finished)
        {
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Play(change, delay, finished));
        }

        IEnumerator Play(Action change, float delay, Action finished)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

            cover.enabled = true;
            yield return Fade(0f, 1f);

            change();
            // One frame with the screen fully black, so the views finish rebuilding before the fade in starts.
            yield return null;

            yield return Fade(1f, 0f);
            cover.enabled = false;
            running = null;
            finished();
        }

        IEnumerator Fade(float from, float to)
        {
            float duration = Mathf.Max(config.fadeSeconds, 0.0001f);
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                SetAlpha(Mathf.Lerp(from, to, t / duration));
                yield return null;
            }
            SetAlpha(to);
        }

        void SetAlpha(float a)
        {
            var c = cover.color;
            c.a = a;
            cover.color = c;
        }
    }
}
