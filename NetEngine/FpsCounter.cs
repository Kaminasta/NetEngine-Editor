namespace NetEngine;

public static class FpsCounter
{
    private static float _accumulatedTime = 0f;
    private static int _frameCount = 0;
    private static float _fps = 0f;

    public static float Fps => _fps;
    public static float UpdateInterval = 0.25f;

    public static void Update()
    {
        _accumulatedTime += Time.DeltaTime;
        _frameCount++;

        if (_accumulatedTime >= UpdateInterval)
        {
            _fps = _frameCount / _accumulatedTime;
            _accumulatedTime = 0f;
            _frameCount = 0;
        }
    }
}
