using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

public class VRPhotoCapture : MonoBehaviour
{
    [Header("UI Referansları")]
    [Tooltip("İçine fotoğrafın aktarılacağı RawImage objesi.")]
    public RawImage photoDisplay; 
    
    [Tooltip("Açıp kapatmak istediğin Ana Canvas veya Panel objesini sürükle.")]
    public GameObject canvasObject; // YENİ: Gizlenecek ana UI objesi

    [Header("Fotoğraf Kamerası")]
    [Tooltip("Oyuncunun veya robotun gözündeki kapalı(disabled) kamerayı sürükle.")]
    public Camera photoCamera; 

    [Header("Kontroller")]
    [Tooltip("Sağ 'A' Tuşu - Fotoğraf Çeker")]
    public InputActionProperty captureAction;
    
    [Tooltip("Sol 'X' Tuşu - Ekranı (Canvas) Açıp Kapatır")]
    public InputActionProperty toggleCanvasAction; // YENİ: Ekran gizleme tuşu

    [Header("Çözünürlük Ayarları")]
    public int imageWidth = 1920;
    public int imageHeight = 1080;

    private Texture2D currentPhoto;

    void OnEnable()
    {
        if (captureAction.action != null) captureAction.action.Enable();
        if (toggleCanvasAction.action != null) toggleCanvasAction.action.Enable();
    }

    void OnDisable()
    {
        if (captureAction.action != null) captureAction.action.Disable();
        if (toggleCanvasAction.action != null) toggleCanvasAction.action.Disable();
    }

    void Update()
    {
        // Sağ A tuşuna basıldığında fotoğraf çek
        if (captureAction.action != null && captureAction.action.WasPressedThisFrame())
        {
            StartCoroutine(TakePhoto());
        }

        // Sol X tuşuna basıldığında Canvas'ı aç/kapat
        if (toggleCanvasAction.action != null && toggleCanvasAction.action.WasPressedThisFrame())
        {
            if (canvasObject != null)
            {
                // Obje açıksa kapatır, kapalıysa açar (! mantığı)
                canvasObject.SetActive(!canvasObject.activeSelf);
            }
        }
    }

    IEnumerator TakePhoto()
    {
        // Eğer fotoğraf çekerken Canvas kapalıysa, çekim bitince görebilmek için otomatik aç (Opsiyonel)
        if (canvasObject != null && !canvasObject.activeSelf)
        {
            canvasObject.SetActive(true);
        }

        yield return new WaitForEndOfFrame();

        RenderTexture rt = new RenderTexture(imageWidth, imageHeight, 24);
        photoCamera.targetTexture = rt;
        photoCamera.Render();
        RenderTexture.active = rt;

        if (currentPhoto != null) Destroy(currentPhoto); 
        currentPhoto = new Texture2D(imageWidth, imageHeight, TextureFormat.RGB24, false);
        currentPhoto.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 0, 0);
        currentPhoto.Apply();

        photoCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        if (photoDisplay != null)
        {
            photoDisplay.texture = currentPhoto;
            photoDisplay.gameObject.SetActive(true);
        }
    }
}