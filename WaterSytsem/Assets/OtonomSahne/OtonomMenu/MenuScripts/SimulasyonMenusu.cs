using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SimulasyonMenusu : MonoBehaviour
{
    [Header("Arayüz (UI) Girdileri")]
    public TMP_InputField liderPortInput;
    public TMP_InputField takipciPortInput;
    public TMP_InputField ortakTickInput;

    public Button baslatButonu;

    [Header("Menü Paneli")]
    public GameObject menuPaneli;

    void Start()
    {
        // Kutularý varsayýlan deðerlerle dolduruyoruz
        // Ortak tick rate tam istediðin gibi 0.125 olarak ayarlandý
        liderPortInput.text = "5007";
        takipciPortInput.text = "5008";
        ortakTickInput.text = "0.125";

        baslatButonu.onClick.AddListener(AyarlariKaydetVeSahneyeGec);
    }

    void AyarlariKaydetVeSahneyeGec()
    {
        // Verileri PlayerPrefs (Kayýt Defteri) üzerine yazýyoruz
        if (int.TryParse(liderPortInput.text, out int lPort))
            PlayerPrefs.SetInt("LiderPort", lPort);

        if (int.TryParse(takipciPortInput.text, out int tPort))
            PlayerPrefs.SetInt("TakipciPort", tPort);

        // Ondalýklý sayýlarda nokta/virgül karmaþasýný engellemek için InvariantCulture kullanýyoruz
        if (float.TryParse(ortakTickInput.text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float tick))
            PlayerPrefs.SetFloat("OrtakTick", tick);

        PlayerPrefs.Save();

        Debug.Log("Ayarlar Kaydedildi, Sahne Yükleniyor...");
        SceneManager.LoadScene("OtonomSahne");
    }
}