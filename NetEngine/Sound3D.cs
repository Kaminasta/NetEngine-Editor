using Silk.NET.OpenAL;
using System.Numerics;
public unsafe class Sound3D
{
    private ALContext _alc;
    private AL _al;
    private uint _source;
    private uint _buffer;

    public Sound3D()
    {
        _alc = ALContext.GetApi();
        _al = AL.GetApi();

        var device = _alc.OpenDevice(null);
        var context = _alc.CreateContext(device, null);
        _alc.MakeContextCurrent(context);
    }

    public AL AL => _al;

    public void Init()
    {
        _buffer = _al.GenBuffer();
        _source = _al.GenSource();

        byte[] wavData;
        int sampleRate;
        short numChannels;
        short bitsPerSample;

        using (FileStream fs = new FileStream(@"D:\UserFolders\Downloads\David-Guetta-Say-My-Name.wav", FileMode.Open, FileAccess.Read))
        using (BinaryReader reader = new BinaryReader(fs))
        {
            fs.Seek(22, SeekOrigin.Begin); 
            numChannels = reader.ReadInt16();

            sampleRate = reader.ReadInt32();

            fs.Seek(34, SeekOrigin.Begin); 
            bitsPerSample = reader.ReadInt16();

            fs.Seek(12, SeekOrigin.Begin);
            while (true)
            {
                string chunkID = new string(reader.ReadChars(4));
                int chunkSize = reader.ReadInt32();
                if (chunkID == "data")
                {
                    wavData = reader.ReadBytes(chunkSize);
                    break;
                }
                else fs.Seek(chunkSize, SeekOrigin.Current);
            }
        }

        if (numChannels == 2 && bitsPerSample == 16)
        {
            short[] stereoSamples = new short[wavData.Length / 2];
            Buffer.BlockCopy(wavData, 0, stereoSamples, 0, wavData.Length);

            int sampleCount = stereoSamples.Length / 2; 
            short[] monoSamples = new short[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                short left = stereoSamples[i * 2];
                short right = stereoSamples[i * 2 + 1];
                monoSamples[i] = (short)((left + right) / 2); 
            }

            wavData = new byte[monoSamples.Length * 2];
            Buffer.BlockCopy(monoSamples, 0, wavData, 0, wavData.Length);

            numChannels = 1; 
        }


        BufferFormat format = GetSoundFormat(numChannels, bitsPerSample);
        fixed (byte* ptr = wavData)
            _al.BufferData(_buffer, format, ptr, wavData.Length, sampleRate);

        _al.SetSourceProperty(
            _source,
            SourceInteger.Buffer,    
            (int)_buffer
        );

        _al.SetSourceProperty(
            _source,
            SourceVector3.Position,  
            0.0f, 0.0f, 0.0f
        );
        //_al.SetListenerProperty(
        //    ListenerVector3.Position,
        //    0.0f, 0.0f, 0.0f
        //);

        //_al.SetSourceProperty(_source, SourceVector3.Position, 0f, 0f, -5f);
        //_al.SetSourceProperty(_source, SourceFloat.Gain, 1.0f);  // Громкость
        _al.SetSourceProperty(_source, SourceBoolean.Looping, true); // Зациклен

        _al.SourcePlay(_source);
    }

    public void UpdateListenerPosition(Vector3 position, Vector3 front, Vector3 up)
    {
        _al.SetListenerProperty(ListenerVector3.Position, position.X, position.Y, position.Z);

        float[] orientation = new float[]
        {
            front.X, front.Y, front.Z,  // направление взгляда
            up.X, up.Y, up.Z            // вектор "вверх"
        };

        fixed (float* pOrientation = orientation)
            _al.SetListenerProperty(ListenerFloatArray.Orientation, pOrientation);
    }


    private BufferFormat GetSoundFormat(short channels, short bits)
    {
        return (channels, bits) switch
        {
            (1, 8) => BufferFormat.Mono8,
            (1, 16) => BufferFormat.Mono16,
            (2, 8) => BufferFormat.Stereo8,
            (2, 16) => BufferFormat.Stereo16,
            _ => throw new NotSupportedException($"Unsupported WAV format: {channels} channels, {bits} bits per sample")
        };
    }
}
