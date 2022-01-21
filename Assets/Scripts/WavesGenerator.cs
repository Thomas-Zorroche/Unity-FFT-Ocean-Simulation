using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class WavesGenerator : MonoBehaviour
{
    // Must be power of 2
    public int _size;

    public bool _computeIFFT2D = true;
    public bool _computeDisp = false;
    public bool _computeNormals = true;
    public bool _recomputeInitials = true;

    public ComputeShader _FFTShader;
    public ComputeShader _initialSpectrumShader;
    public ComputeShader _timeDependentSpectrumShader;
    public ComputeShader _wavesDisplacementShader;
    public ComputeShader _normalsShader;

    public Vector2 _windDirection;
    public float _windSpeed;

    public uint _wavesCascadesCount;
    public float _lengthscale;
    public float _dispStrength;
    public float _exponent;
    public float _smallWaves;
    public float _amplitude;
    public float _L;
    public float _g;

    public List<WavesCascade> _waves;
    public FastFourierTransform _FFT;

    private Texture2D _noise;

    void Awake()
    {
        _FFT = new FastFourierTransform(_size, _FFTShader);
        _noise = GetNoiseTexture();

        _waves = new List<WavesCascade>();
        _waves.Add(new WavesCascade(_size, _FFT, _initialSpectrumShader, _timeDependentSpectrumShader, 
            _wavesDisplacementShader, _normalsShader, _windDirection, _windSpeed, _noise, _lengthscale));

        InitializeWaves();
    }


    private void InitializeWaves()
    {
        foreach (var wave in _waves)
        {
            wave.ComputeInitialSpectrum(_windDirection, _windSpeed, _lengthscale, 
                _exponent, _smallWaves, _amplitude, _L, _g);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (_recomputeInitials)
        {
            Shader.SetGlobalFloat("_lengthScale", _lengthscale);
            Shader.SetGlobalFloat("_dispStrength", _dispStrength);
            InitializeWaves();
        }

        foreach (var wave in _waves)
        {
            
            wave.UpdateWaves(Time.time, _computeIFFT2D, _computeDisp, _computeNormals, _lengthscale, _L, _g);
        }
    }

    private void OnDestroy()
    {
        _FFT.Destroy();
    }

    private Texture2D GetNoiseTexture()
    {
        string filename = "Textures/Noise" + _size.ToString();
        Texture2D noise = Resources.Load<Texture2D>(filename);

        if (noise == null)
            Debug.Log("No Noise texture.");

        return noise ? noise : CreateNoiseTexure();
    }

    float NormalRandom()
    {
        return Mathf.Cos(2 * Mathf.PI * Random.value) * Mathf.Sqrt(-2 * Mathf.Log(Random.value));
    }

    private Texture2D CreateNoiseTexure()
    {
        Texture2D noise = new Texture2D(_size, _size, TextureFormat.RGFloat, false, true);
        noise.filterMode = FilterMode.Point;
        for (int i = 0; i < _size; i++)
        {
            for (int j = 0; j < _size; j++)
            {
                noise.SetPixel(i, j, new Vector4(NormalRandom(), NormalRandom()));
            }
        }
        noise.Apply();

        string filename = "Assets/Resources/Textures/Noise" + _size.ToString();
        AssetDatabase.CreateAsset(noise, filename + ".asset");
        Debug.Log(filename + "was created.");
      
        return noise;
    }
}
