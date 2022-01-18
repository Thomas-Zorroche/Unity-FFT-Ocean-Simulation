using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public class FastFourierTransform
{
    private ComputeShader _FFTComputeShader;

    private int _size;
    public RenderTexture _butterflyTexture;

    readonly int LOCAL_WORK_GROUPS = 8; // 8 x 8 x 1

    List<int> _bitReversedIndices;
    ComputeBuffer _reversedIndicesBuffer;


    public FastFourierTransform(int size, ComputeShader shader)
    {
        _size = size;

        _FFTComputeShader = shader;
        KERNEL_BUTTERFLY = _FFTComputeShader.FindKernel("CreateButterflyTexture");
        KERNEL_HORIZONTAL_INVERSE_IFFT = _FFTComputeShader.FindKernel("HorizontalInverseFFT");
        KERNEL_VERTICAL_INVERSE_IFFT = _FFTComputeShader.FindKernel("VerticalInverseFFT");
        KERNEL_PERMUTE_IFFT = _FFTComputeShader.FindKernel("Permute");

        CreateBitReversedIndices();
        _butterflyTexture = CreateButterflyTexture();
        //TextureUtils.SavePNG(_butterflyTexture, "ButterflyTexture");
    }

    public void Destroy()
    {
        _reversedIndicesBuffer.Dispose();
    }

    private RenderTexture CreateButterflyTexture()
    {
        // (Log2(_size) * _size) texture
        int width = (int)Mathf.Log(_size, 2);
        RenderTexture rt = new RenderTexture(width, _size, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);

        rt.wrapMode = TextureWrapMode.Repeat;
        rt.filterMode = FilterMode.Point;
        rt.enableRandomWrite = true;
        rt.Create();

        _FFTComputeShader.SetInt(CS_ID_SIZE, _size);
        _FFTComputeShader.SetTexture(KERNEL_BUTTERFLY, CS_ID_BUTTERFLY_TEXTURE, rt);
        _FFTComputeShader.SetBuffer(KERNEL_BUTTERFLY, CS_ID_REVERSED_INDICES, _reversedIndicesBuffer);
        _FFTComputeShader.Dispatch(KERNEL_BUTTERFLY, width, _size / LOCAL_WORK_GROUPS /* 8 */, 1);

        return rt;
    }

    private static string Reverse(string s)
    {
        char[] charArray = s.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }

    private void CreateBitReversedIndices()
    {
        _bitReversedIndices = new List<int>();
        int bits = (int)(Mathf.Log(_size) / Mathf.Log(2.0f));
        for (int i = 0; i < _size; i++)
        {
            string binaryString = Convert.ToString(i, 2).PadLeft(bits, '0');
            int reversed = Convert.ToInt32(Reverse(binaryString), 2);
            _bitReversedIndices.Add(reversed);
            //Debug.Log(reversed);
        }

        _reversedIndicesBuffer = new ComputeBuffer(_size, sizeof(int));
        _reversedIndicesBuffer.SetData(_bitReversedIndices);
    }

    public void IFFT2D(RenderTexture input, RenderTexture buffer/*, RenderTexture displacement*/)
    {
        int logSize = (int)Mathf.Log(_size, 2);
        bool pingpong = false;

        // Horizontal
        _FFTComputeShader.SetTexture(KERNEL_HORIZONTAL_INVERSE_IFFT, CS_ID_BUTTERFLY_TEXTURE, _butterflyTexture);
        _FFTComputeShader.SetTexture(KERNEL_HORIZONTAL_INVERSE_IFFT, CS_ID_BUFFER0, input);
        _FFTComputeShader.SetTexture(KERNEL_HORIZONTAL_INVERSE_IFFT, CS_ID_BUFFER1, buffer);
        for (int i = 0; i < logSize; i++)
        {
            pingpong = !pingpong;
            _FFTComputeShader.SetInt(CS_ID_STEP, i);
            _FFTComputeShader.SetBool(CS_ID_PINGPONG, pingpong);
            _FFTComputeShader.Dispatch(KERNEL_HORIZONTAL_INVERSE_IFFT, _size / LOCAL_WORK_GROUPS, _size / LOCAL_WORK_GROUPS, 1);
        }

        // Vertical
        _FFTComputeShader.SetTexture(KERNEL_VERTICAL_INVERSE_IFFT, CS_ID_BUTTERFLY_TEXTURE, _butterflyTexture);
        _FFTComputeShader.SetTexture(KERNEL_VERTICAL_INVERSE_IFFT, CS_ID_BUFFER0, input);
        _FFTComputeShader.SetTexture(KERNEL_VERTICAL_INVERSE_IFFT, CS_ID_BUFFER1, buffer);
        for (int i = 0; i < logSize; i++)
        {
            pingpong = !pingpong;
            _FFTComputeShader.SetInt(CS_ID_STEP, i);
            _FFTComputeShader.SetBool(CS_ID_PINGPONG, pingpong);
            _FFTComputeShader.Dispatch(KERNEL_VERTICAL_INVERSE_IFFT, _size / LOCAL_WORK_GROUPS, _size / LOCAL_WORK_GROUPS, 1);
        }

        if (pingpong)
            Graphics.Blit(buffer, input);

        // Permutation
       _FFTComputeShader.SetTexture(KERNEL_PERMUTE_IFFT, CS_ID_BUFFER0, input);
       _FFTComputeShader.Dispatch(KERNEL_PERMUTE_IFFT, _size / LOCAL_WORK_GROUPS, _size / LOCAL_WORK_GROUPS, 1);
    }


    // FFT compute shader kernels Ids
    readonly int KERNEL_BUTTERFLY;
    readonly int KERNEL_HORIZONTAL_INVERSE_IFFT;
    readonly int KERNEL_VERTICAL_INVERSE_IFFT;
    readonly int KERNEL_PERMUTE_IFFT;

    // FFT compute shader uniforms Ids
    readonly int CS_ID_BUTTERFLY_TEXTURE = Shader.PropertyToID("ButterflyTexture");
    readonly int CS_ID_REVERSED_INDICES = Shader.PropertyToID("ReversedIndices");
    readonly int CS_ID_SIZE = Shader.PropertyToID("Size");

    readonly int CS_ID_BUFFER0 = Shader.PropertyToID("Buffer0");
    readonly int CS_ID_BUFFER1 = Shader.PropertyToID("Buffer1");
    readonly int CS_ID_STEP = Shader.PropertyToID("Step");
    readonly int CS_ID_PINGPONG = Shader.PropertyToID("Pingpong");
    readonly int CS_ID_DISPLACEMENT = Shader.PropertyToID("Displacement");



}
