using UnityEngine;

namespace VoidConverter.View
{
    /// <summary>Plays the frames of a sprite sheet in a loop on a SpriteRenderer (an idle animation).</summary>
    public class SpriteFrames : MonoBehaviour
    {
        SpriteRenderer target;
        Sprite[] frames;
        float framesPerSecond;
        float timeOffset;

        /// <summary>
        /// <paramref name="offset"/> shifts the start in seconds, so neighbours of the same kind do not move in step.
        /// The clock is the global one, so a picture that is rebuilt keeps its place in the loop.
        /// </summary>
        public void Init(SpriteRenderer renderer, Sprite[] sheetFrames, float fps, float offset)
        {
            target = renderer;
            frames = sheetFrames;
            framesPerSecond = fps;
            timeOffset = offset;
            Show();
        }

        void Update() => Show();

        void Show()
        {
            if (target == null || frames == null || frames.Length < 2) return;
            int index = Mathf.FloorToInt((Time.unscaledTime + timeOffset) * framesPerSecond) % frames.Length;
            target.sprite = frames[index];
        }
    }
}
