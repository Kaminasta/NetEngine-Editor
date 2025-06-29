using Silk.NET.OpenAL;
using System.Numerics;

namespace NetEngine.Components;

public unsafe class AudioSource : Behaviour
{
    private ALContext _alc;
    private AL _al;
    private uint _source;
    private uint _buffer;

    [ShowInInspector]
    [Range(0f,1f)]
    public float Volume = 1f;
    [ShowInInspector]
    public float Pitch = 1f;
    [ShowInInspector]
    public bool Loop = false;

    public AudioSource()
    {
        _alc = ALContext.GetApi();
        _al = AL.GetApi();

        var device = _alc.OpenDevice(null);
        var context = _alc.CreateContext(device, null);
        _alc.MakeContextCurrent(context);

        Init();
    }

    private void Init()
    {
        _buffer = _al.GenBuffer();
        _source = _al.GenSource();

        var (data, sampleRate, channels, bitsPerSample) = LoadWav("D:\\UserFolders\\Downloads\\David-Guetta-Say-My-Name.wav");
        var format = GetSoundFormat(channels, bitsPerSample);

        fixed (byte* ptr = data)
            _al.BufferData(_buffer, format, ptr, data.Length, sampleRate);

        _al.SetSourceProperty(_source, SourceInteger.Buffer, (int)_buffer);

        Play();
    }

    public void Play() => _al.SourcePlay(_source);
    public void Pause() => _al.SourcePause(_source);
    public void Stop() => _al.SourceStop(_source);

    public override void Update()
    {
        if (GameObject?.Transform != null)
        {
            var pos = GameObject.Transform.Position;
            _al.SetSourceProperty(_source, SourceVector3.Position, pos.X, pos.Y, pos.Z);
        }

        _al.SetSourceProperty(_source, SourceFloat.Gain, Volume);
        _al.SetSourceProperty(_source, SourceFloat.Pitch, Pitch);
        _al.SetSourceProperty(_source, SourceBoolean.Looping, Loop);
    }

    private (byte[] data, int sampleRate, short channels, short bits) LoadWav(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(fs);

        fs.Seek(22, SeekOrigin.Begin);
        short channels = reader.ReadInt16();
        int sampleRate = reader.ReadInt32();
        fs.Seek(34, SeekOrigin.Begin);
        short bits = reader.ReadInt16();

        fs.Seek(12, SeekOrigin.Begin);
        byte[] data = Array.Empty<byte>();
        while (true)
        {
            string chunkID = new string(reader.ReadChars(4));
            int chunkSize = reader.ReadInt32();
            if (chunkID == "data")
            {
                data = reader.ReadBytes(chunkSize);
                break;
            }
            fs.Seek(chunkSize, SeekOrigin.Current);
        }

        // Конвертация стерео в моно
        if (channels == 2 && bits == 16)
        {
            short[] stereo = new short[data.Length / 2];
            Buffer.BlockCopy(data, 0, stereo, 0, data.Length);

            int samples = stereo.Length / 2;
            short[] mono = new short[samples];
            for (int i = 0; i < samples; i++)
                mono[i] = (short)((stereo[i * 2] + stereo[i * 2 + 1]) / 2);

            data = new byte[mono.Length * 2];
            Buffer.BlockCopy(mono, 0, data, 0, data.Length);
            channels = 1;
        }

        return (data, sampleRate, channels, bits);
    }

    private BufferFormat GetSoundFormat(short ch, short bits) => (ch, bits) switch
    {
        (1, 8) => BufferFormat.Mono8,
        (1, 16) => BufferFormat.Mono16,
        (2, 8) => BufferFormat.Stereo8,
        (2, 16) => BufferFormat.Stereo16,
        _ => throw new NotSupportedException($"{ch}ch, {bits}bit")
    };
}
