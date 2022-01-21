using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WavesCascade
{

    private int _size;
    private FastFourierTransform _FFT;



    readonly ComputeShader _initialSpectrumShader;
    public ComputeShader _timeDependentSpectrumShader;
    public ComputeShader _wavesDisplacementShader;

    private RenderTexture _initialSpectrumTexture;
    private RenderTexture _initialSpectrumMinusKTexture;

    public RenderTexture _HK_Dx;
    public RenderTexture _HK_Dy;
    public RenderTexture _HK_Dz;

    public RenderTexture _displacement;

    private Texture2D _noise;

    readonly int LOCAL_WORK_GROUPS = 8;

    public WavesCascade(int size, FastFourierTransform FFT, ComputeShader initialSpectrumShader, 
        ComputeShader timeDependentSpectrumShader, ComputeShader wavesDisplacementShader, Vector2 windDirection, 
        float windSpeed, Texture2D noise, float lenghtScale)
    {
        _size = size;
        _FFT = FFT;

        _initialSpectrumShader = initialSpectrumShader;
        _timeDependentSpectrumShader = timeDependentSpectrumShader;
        _wavesDisplacementShader = wavesDisplacementShader;

        _initialSpectrumTexture = TextureUtils.CreateRenderTexture(_size, RenderTextureFormat.ARGBFloat);
        _initialSpectrumMinusKTexture = TextureUtils.CreateRenderTexture(_size, RenderTextureFormat.ARGBFloat);
        _noise = noise;
        _displacement = TextureUtils.CreateRenderTexture(_size, RenderTextureFormat.ARGBFloat);

        _HK_Dx = TextureUtils.CreateRenderTexture(_size);
        _HK_Dy = TextureUtils.CreateRenderTexture(_size);
        _HK_Dz = TextureUtils.CreateRenderTexture(_size);

        KERNEL_INITIAL_SPECTRUM = _initialSpectrumShader.FindKernel("ComputeInitialSpectrum");
        KERNEL_TIME_DEPENDENT_SPECTRUM = _timeDependentSpectrumShader.FindKernel("ComputeTimeDependentSpectrum");
        KERNEL_WAVES_DISPLACEMENT = _wavesDisplacementShader.FindKernel("WavesDisplacement");
    }

    // Compute TF(h0(k)) and TF(h0(-k))
    public void ComputeInitialSpectrum(Vector2 windDirection, float windSpeed, float lengthScale, 
        float exponent, float smallWaves, float amplitude, float L, float g)
    {
        _initialSpectrumShader.SetInt(CS_ID_SIZE, _size);
        _initialSpectrumShader.SetFloat(CS_ID_WIND_SPEED, windSpeed);
        _initialSpectrumShader.SetFloat(CS_ID_LENGTH_SCALE, lengthScale);
        _initialSpectrumShader.SetFloat(CS_ID_EXPONENT, exponent);
        _initialSpectrumShader.SetFloat(CS_ID_SMALL_WAVES, smallWaves);
        _initialSpectrumShader.SetFloat(CS_ID_AMPLITUDE, amplitude);
        _initialSpectrumShader.SetFloat(CS_ID_L, L);
        _initialSpectrumShader.SetFloat(CS_ID_G, g);
        _initialSpectrumShader.SetVector(CS_ID_WIND_DIRECTION, windDirection);
        _initialSpectrumShader.SetTexture(KERNEL_INITIAL_SPECTRUM, CS_ID_NOISE, _noise);
        _initialSpectrumShader.SetTexture(KERNEL_INITIAL_SPECTRUM, CS_ID_H0_TILDE, _initialSpectrumTexture);
        _initialSpectrumShader.SetTexture(KERNEL_INITIAL_SPECTRUM, CS_ID_H0_TILDE_MINUS_K, _initialSpectrumMinusKTexture);
        _initialSpectrumShader.Dispatch(KERNEL_INITIAL_SPECTRUM, _size / LOCAL_WORK_GROUPS, _size / LOCAL_WORK_GROUPS, 1);

        //TextureUtils.SavePNG(_initialSpectrumTexture, "InitialSpectrum");
        //TextureUtils.SavePNG(_initialSpectrumMinusKTexture, "InitialSpectrum_minusk");
    }

    public void UpdateWaves(float time, bool computeIFFT2D, bool computeDisp, float lengthScale, float L, float g)
    {
        // Compute TF(h(k))
        _timeDependentSpectrumShader.SetTexture(KERNEL_TIME_DEPENDENT_SPECTRUM, CS_ID_HK_Dx, _HK_Dx);
        _timeDependentSpectrumShader.SetTexture(KERNEL_TIME_DEPENDENT_SPECTRUM, CS_ID_HK_Dy, _HK_Dy);
        _timeDependentSpectrumShader.SetTexture(KERNEL_TIME_DEPENDENT_SPECTRUM, CS_ID_HK_Dz, _HK_Dz);
        _timeDependentSpectrumShader.SetTexture(KERNEL_TIME_DEPENDENT_SPECTRUM, CS_ID_H0_TILDE, _initialSpectrumTexture);
        _timeDependentSpectrumShader.SetTexture(KERNEL_TIME_DEPENDENT_SPECTRUM, CS_ID_H0_TILDE_MINUS_K, _initialSpectrumMinusKTexture);
        _timeDependentSpectrumShader.SetFloat(CS_ID_SIZE, _size);
        _timeDependentSpectrumShader.SetFloat(CS_ID_TIME, time);
        _timeDependentSpectrumShader.SetFloat(CS_ID_LENGTH_SCALE, lengthScale);
        _timeDependentSpectrumShader.SetFloat(CS_ID_L, L);
        _timeDependentSpectrumShader.SetFloat(CS_ID_G, g);
        _timeDependentSpectrumShader.Dispatch(KERNEL_TIME_DEPENDENT_SPECTRUM, _size / LOCAL_WORK_GROUPS, _size / LOCAL_WORK_GROUPS, 1);

        // Compute IFFT2D 
        if (computeIFFT2D)
        {
            _FFT.IFFT2D(_HK_Dx, _initialSpectrumTexture);
            _FFT.IFFT2D(_HK_Dy, _initialSpectrumTexture);
            _FFT.IFFT2D(_HK_Dz, _initialSpectrumTexture);
        }

        // Compute displacement
        if (computeDisp)
        {
            _wavesDisplacementShader.SetTexture(KERNEL_WAVES_DISPLACEMENT, CS_ID_DISP, _displacement);
            _wavesDisplacementShader.SetTexture(KERNEL_WAVES_DISPLACEMENT, CS_ID_HK_Dx, _HK_Dx);
            _wavesDisplacementShader.SetTexture(KERNEL_WAVES_DISPLACEMENT, CS_ID_HK_Dy, _HK_Dy);
            _wavesDisplacementShader.SetTexture(KERNEL_WAVES_DISPLACEMENT, CS_ID_HK_Dz, _HK_Dz);
            _wavesDisplacementShader.Dispatch(KERNEL_WAVES_DISPLACEMENT, _size / LOCAL_WORK_GROUPS, _size / LOCAL_WORK_GROUPS, 1);
        }
    }


    // Compute shader kernels Ids
    readonly int KERNEL_INITIAL_SPECTRUM;
    readonly int KERNEL_TIME_DEPENDENT_SPECTRUM;
    readonly int KERNEL_WAVES_DISPLACEMENT;

    // Compute shader uniforms Ids
    readonly int CS_ID_SIZE = Shader.PropertyToID("Size");
    //readonly int CS_ID_A = Shader.PropertyToID("A");
    readonly int CS_ID_WIND_DIRECTION = Shader.PropertyToID("windDirection");
    readonly int CS_ID_WIND_SPEED = Shader.PropertyToID("windSpeed");
    readonly int CS_ID_NOISE = Shader.PropertyToID("Noise");
    readonly int CS_ID_LENGTH_SCALE = Shader.PropertyToID("lengthScale");

    readonly int CS_ID_EXPONENT = Shader.PropertyToID("exponent");
    readonly int CS_ID_SMALL_WAVES = Shader.PropertyToID("smallWaves");
    readonly int CS_ID_AMPLITUDE = Shader.PropertyToID("amplitude");
    readonly int CS_ID_L = Shader.PropertyToID("L");
    readonly int CS_ID_G = Shader.PropertyToID("g");

    readonly int CS_ID_H0_TILDE = Shader.PropertyToID("H0_K_tilde");
    readonly int CS_ID_H0_TILDE_MINUS_K = Shader.PropertyToID("H0_MINUSK_tilde");

    readonly int CS_ID_HK_Dx = Shader.PropertyToID("HK_Dx");
    readonly int CS_ID_HK_Dy = Shader.PropertyToID("HK_Dy");
    readonly int CS_ID_HK_Dz = Shader.PropertyToID("HK_Dz");
    
    readonly int CS_ID_TIME = Shader.PropertyToID("Time");
    readonly int CS_ID_DISP = Shader.PropertyToID("Displacement");
    


}
