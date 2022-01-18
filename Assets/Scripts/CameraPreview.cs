using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPreview : MonoBehaviour
{
    public enum TexturePreview
    {
        Hk_Dx = 0,
        Hk_Dy = 1,
        Hk_Dz = 2,
        Displacement = 3,
        Butterfly
    }

    public TexturePreview _texturePreview;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        var ocean = GameObject.FindObjectOfType<WavesGenerator>();
        if (ocean)
        {
            RenderTexture texture;
            switch (_texturePreview)
            {
                case TexturePreview.Hk_Dx:
                    texture = ocean._waves[0]._HK_Dx;   
                    break;
                case TexturePreview.Hk_Dy:
                    texture = ocean._waves[0]._HK_Dy;
                    break;
                case TexturePreview.Hk_Dz:
                    texture = ocean._waves[0]._HK_Dz;
                    break;
                case TexturePreview.Displacement:
                    texture = ocean._waves[0]._displacement;
                    break;
                case TexturePreview.Butterfly:
                    texture = ocean._FFT._butterflyTexture;
                    break;
                default:
                    texture = ocean._waves[0]._displacement;
                    break;
            }

            Graphics.Blit(texture, destination);

        }

    }
}
