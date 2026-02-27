using UnityEngine;

[DisallowMultipleComponent]
public class SpriteFlipbook : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private SpriteRenderer target;

    [Header("Frames")]
    [SerializeField] private Sprite[] frames;

    [Header("Timing")]
    [Min(0.01f)] [SerializeField] private float fps = 12f;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private float speed = 1f;

    private int _frameIndex;
    private float _t;
    private bool _playing;

    void Reset()
    {
        target = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        if (playOnEnable) Play(true);
        else ApplyFrame();
    }

    void Update()
    {
        if (!_playing || target == null || frames == null || frames.Length == 0)
            return;

        float frameTime = 1f / Mathf.Max(0.01f, fps);

        _t += Time.deltaTime * speed;

        while (_t >= frameTime)
        {
            _t -= frameTime;
            StepFrame();
        }
    }

    public void Play(bool restart = false)
    {
        if (restart)
        {
            _frameIndex = 0;
            _t = 0f;
        }

        _playing = true;
        ApplyFrame();
    }

    public void Stop(bool keepFrame = true)
    {
        _playing = false;
        if (!keepFrame && target != null) target.sprite = null;
    }

    public void SetFrames(Sprite[] newFrames, bool restart = true)
    {
        frames = newFrames;
        if (restart) Play(true);
        else ApplyFrame();
    }

    public void SetNormalizedTime(float normalized)
    {
        if (frames == null || frames.Length == 0) return;

        normalized = Mathf.Repeat(normalized, 1f);
        _frameIndex = Mathf.Clamp(Mathf.FloorToInt(normalized * frames.Length), 0, frames.Length - 1);
        _t = 0f;
        ApplyFrame();
    }

    private void StepFrame()
    {
        _frameIndex++;

        if (_frameIndex >= frames.Length)
        {
            if (loop) _frameIndex = 0;
            else
            {
                _frameIndex = frames.Length - 1;
                _playing = false;
            }
        }

        ApplyFrame();
    }

    private void ApplyFrame()
    {
        if (target == null || frames == null || frames.Length == 0) return;
        _frameIndex = Mathf.Clamp(_frameIndex, 0, frames.Length - 1);
        target.sprite = frames[_frameIndex];
    }
}
