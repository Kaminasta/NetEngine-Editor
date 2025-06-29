using Silk.NET.OpenAL;
using System.Numerics;

namespace NetEngine.Components;

public unsafe class AudioListener : Behaviour
{
    private AL _al;

    public AudioListener()
    {
        _al = AL.GetApi();
    }

    public override void Update()
    {
        if (GameObject?.Transform != null)
        {
            Vector3 pos = GameObject.Transform.Position;
            Vector3 forward = GameObject.Transform.Front;
            Vector3 up = GameObject.Transform.Up;

            _al.SetListenerProperty(ListenerVector3.Position, pos.X, pos.Y, pos.Z);

            float[] orientation =
            [
                forward.X, forward.Y, forward.Z,
                up.X, up.Y, up.Z
            ];

            fixed (float* ptr = orientation)
                _al.SetListenerProperty(ListenerFloatArray.Orientation, ptr);
        }
    }
}
