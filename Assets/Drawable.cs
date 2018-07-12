using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class Drawable : MonoBehaviour {

    private RawImage image;
    [SerializeField]
    private Color drawColor = Color.red;

    private float pixelWidth;
    
	private void Start() {
        image = GetComponent<RawImage>();

        // How big a pixel of this image is on the screen
        pixelWidth = image.rectTransform.rect.width / image.texture.width;

        // Recreate original image so that doesn't get changed in the files
        Texture2D newTexture = new Texture2D(image.texture.width, image.texture.height, TextureFormat.ARGB32, false);
        for (int i = 0; i < image.texture.width; i++) {
            for (int k = 0; k < image.texture.height; k++) {
                newTexture.SetPixel(i, k, ((Texture2D) image.texture).GetPixel(i, k));
            }
        }

        newTexture.Apply();
        image.texture = newTexture;
	}
	
	private void Update() {
		if (Input.touchCount > 0) {
            for (int i = 0; i < Input.touchCount; i++) {
                Touch currTouch = Input.GetTouch(i);
                
                if (image.rectTransform.rect.Contains(currTouch.position)) {
                    Vector2 coordInImage = new Vector2((currTouch.position.x - image.rectTransform.position.x) / pixelWidth,
                        (currTouch.position.y - image.rectTransform.position.y) / pixelWidth);

                    ((Texture2D) image.texture).SetPixel((int) coordInImage.x, (int) coordInImage.y, drawColor);
                }
            }

            ((Texture2D) image.texture).Apply();
        }
	}
}
