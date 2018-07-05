using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class Distortable : MonoBehaviour {

    // The distortable image
    private Image image;
    private Texture2D texture;

    // The image and texture for the distort target
    public Image distortTarget;
    private Texture2D targetTexture;

	private void Start () {
        image = GetComponent<Image>();

        texture = (Texture2D) image.mainTexture;
        // Create new image for the distoring
        targetTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        
        // Apply modifications to distorted image and set the target
        targetTexture.Apply();
        distortTarget.material.mainTexture = targetTexture;
	}
}
