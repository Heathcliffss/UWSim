using UnityEngine;

public class SimulasyonVeriAlici : MonoBehaviour
{
    [Header("Robot Script Referanslarý")]
    public GazeboDataReceiver liderDataReceiver;
    public GazeboDataReceiver takipciDataReceiver;
    public RovLeds liderLeds;
    public RovLeds takipciLeds;

    // UDP dinlemesi Start'ta baþladýðý için, verileri ondan ÖNCE (Awake'te) aktarmalýyýz.
    void Awake()
    {
        // Hafýzadaki verileri çek (Eðer menüden gelinmediyse saðdaki varsayýlan deðerleri kullanýr)
        int lPort = PlayerPrefs.GetInt("LiderPort", 5007);
        int tPort = PlayerPrefs.GetInt("TakipciPort", 5008);
        float tick = PlayerPrefs.GetFloat("OrtakTick", 0.125f);

        // --- ROBOTLARA VERÝLERÝ AKTAR ---
        if (liderDataReceiver != null) liderDataReceiver.listenPort = lPort;
        if (takipciDataReceiver != null) takipciDataReceiver.listenPort = tPort;

        if (liderLeds != null) liderLeds.tickRate = tick;
        if (takipciLeds != null) takipciLeds.tickRate = tick;

        Debug.Log($"[OTONOM SAHNE YÜKLENDÝ] Lider Port: {lPort} | Takipçi Port: {tPort} | Tick: {tick}");
    }
}