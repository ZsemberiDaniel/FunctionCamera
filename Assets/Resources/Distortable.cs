using UnityEngine;
using UnityEngine.UI;
using static MathHelper;

[RequireComponent(typeof(RawImage))]
public class Distortable : MonoBehaviour {

    // The distortable image
    private RawImage image;
    private Texture2D texture;

    // The image and texture for the distort target
    public RawImage distortTarget;
    private Texture2D targetTexture;

	private void Start() {
        image = GetComponent<RawImage>();

        texture = (Texture2D) image.mainTexture;
        // Create new image for the distoring
        targetTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
	}

    /// <summary>
    /// Distort the image to the target texture based on an expression tree
    /// </summary>
    public void DistortCenterBottomLeft(ExpressionTree tree) {
        // Create new image for the distoring
        targetTexture = new Texture2D(texture.width * 2, texture.height * 2, TextureFormat.RGBA32, false);
        for (int i = 0; i < targetTexture.width; i++) {
            for (int k = 0; k < targetTexture.height; k++) {
                targetTexture.SetPixel(i, k, Color.white);
            }
        }

        Number newCoord;

        // Go through each pixel
        for (int i = 0; i < texture.width; i++) {
            for (int k = 0; k <= texture.height; k++) {
                newCoord = tree.Evaluate(i, k);

                targetTexture.SetPixel((int) newCoord.number, (int) newCoord.i, 
                    texture.GetPixel(i, k));
            }
        }

        // Apply modifications to distorted image and set the target
        targetTexture.Apply();
        distortTarget.texture = targetTexture;
    }

    public void DistortCenterMiddle(ExpressionTree tree) {
        // Create new image for the distoring
        targetTexture = new Texture2D(texture.width * 10, texture.height * 10, TextureFormat.RGBA32, false);

        Number newCoord;

        // Go through each pixel
        for (int i = -texture.width / 2; i < texture.width / 2; i++) {
            for (int k = -texture.height / 2; k < texture.height / 2; k++) {
                newCoord = tree.Evaluate(i, k);
                // Debug.Log(i + " " + k + " -> " + newCoord.number + " " + newCoord.i);

                targetTexture.SetPixel((int) newCoord.number + targetTexture.width / 2, (int) newCoord.i + targetTexture.height / 2,
                    texture.GetPixel(i + texture.width / 2, k + texture.height / 2));
            }
        }

        // Apply modifications to distorted image and set the target
        targetTexture.Apply();
        distortTarget.texture = targetTexture;
    }
}
