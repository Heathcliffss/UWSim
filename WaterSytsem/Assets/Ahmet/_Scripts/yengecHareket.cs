using UnityEngine;
using System.Collections;

public class yengecHareket : MonoBehaviour
{
    public float gezinmeYaricapi = 5f;
    public float beklemeSuresi = 2f;
    public float hareketHizi = 2f;

    private Vector3 hedefNokta;
    private bool hareketEdiyor = false;
    private Transform yengecHareketContainer;

    void Start()
    {
        // 1. Mevcut yengeç modelini referans al
        Transform yengecModel = this.transform;

        // 2. Hareketleri yönetecek "temiz" bir taşıyıcı oluştur
        yengecHareketContainer = new GameObject("YengecHareketContainer").transform;
        yengecHareketContainer.position = yengecModel.position;
        yengecHareketContainer.rotation = Quaternion.identity; // Sıfır rotasyon

        // 3. Yengeci bu taşıyıcının içine koy
        yengecModel.SetParent(yengecHareketContainer);

        // 4. MODELİ DÜZELT: 
        // Senin modelin Blender'dan geldiği için -90 derecede düz duruyor.
        // Bir önceki denemede 0 yapınca yengeç ters dönmüştü.
        // Şimdi onu -90 derecede sabitliyoruz.
        yengecModel.localRotation = Quaternion.Euler(-90, 0, 0);

        YeniHedefSec();
    }

    void Update()
    {
        // Hedefe ulaştıysak ve bekleme bitmişse yeni hedef seç
        if (Vector3.Distance(yengecHareketContainer.position, hedefNokta) < 0.3f && !hareketEdiyor)
        {
            StartCoroutine(BekleVeYeniHedefSec());
        }

        // Hareket mantığı
        if (hedefNokta != Vector3.zero)
        {
            // Taşıyıcıyı (Container) hedefe döndür
            Vector3 yon = hedefNokta - yengecHareketContainer.position;
            yon.y = 0; // Yere paralel kalsın

            if (yon != Vector3.zero)
            {
                Quaternion bakisAcisi = Quaternion.LookRotation(yon);
                yengecHareketContainer.rotation = Quaternion.Slerp(yengecHareketContainer.rotation, bakisAcisi, Time.deltaTime * 3f);
            }

            // Taşıyıcıyı (ve içindeki yengeci) ileri yürüt
            yengecHareketContainer.Translate(Vector3.forward * hareketHizi * Time.deltaTime);
        }
    }

    void YeniHedefSec()
    {
        Vector3 rastgeleYon = Random.insideUnitSphere * gezinmeYaricapi;
        rastgeleYon += yengecHareketContainer.position;
        rastgeleYon.y = yengecHareketContainer.position.y;
        hedefNokta = rastgeleYon;
    }

    IEnumerator BekleVeYeniHedefSec()
    {
        hareketEdiyor = true;
        yield return new WaitForSeconds(beklemeSuresi);
        YeniHedefSec();
        hareketEdiyor = false;
    }
}