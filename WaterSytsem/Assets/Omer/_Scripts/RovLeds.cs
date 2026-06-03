using UnityEngine;

public class RovLeds : MonoBehaviour
{
    [Header("Iþýk Gruplarý")]
    public GameObject[] frontLEDs;
    public GameObject[] backLEDs;
    public GameObject[] leftLEDs;
    public GameObject[] rightLEDs;

    [Header("Test Ayarlarý")]
    public bool isEmergency = false;

    // Mevcut back-only pattern testi için korunuyor.
    public bool backOnlyTest = true;

    // Yeni test modu:
    // true yapýlýrsa sadece BACK LED'ler sürekli açýk kalýr.
    // Pattern/blink uygulanmaz.
    public bool forceBackConstantOn = false;

    [Header("Zamanlama Ayarý")]
    public int targetFPS = 60;

    // SimulasyonVeriAlici.cs bu deðiþkene eriþtiði için korunuyor.
    public float tickRate = 0.1f;

    [Header("Frame Tabanlý Pattern Ayarý")]
    public int framesPerBit = 6;

    private string pFront = "11110000";
    private string pBack = "11001100";
    private string pLeft = "10101010";
    private string pRight = "10011001";
    private string pEmer = "1111111100000000";

    private int startFrame;

    void Start()
    {
        Application.targetFrameRate = targetFPS;
        startFrame = Time.frameCount;

        UpdateFramesPerBit();
    }

    void Update()
    {
        // Eðer baþka script tickRate'i deðiþtirirse framesPerBit de güncel kalsýn.
        UpdateFramesPerBit();

        // ============================================================
        // 1) CONSTANT ON BACK TEST MODE
        // Bu mod bizim temiz tracking/control videosu için.
        // Sadece arka iki LED sürekli açýk kalýr.
        // Diðer yüzler kapalýdýr.
        // Pattern/blink yoktur.
        // ============================================================
        if (forceBackConstantOn)
        {
            SetGroup(frontLEDs, false);
            SetGroup(backLEDs, true);
            SetGroup(leftLEDs, false);
            SetGroup(rightLEDs, false);
            return;
        }

        int elapsedFrames = Time.frameCount - startFrame;
        int bitIndex = elapsedFrames / framesPerBit;

        // ============================================================
        // 2) EMERGENCY MODE
        // Tüm LED gruplarý emergency pattern ile yanýp söner.
        // ============================================================
        if (isEmergency)
        {
            bool state = pEmer[bitIndex % pEmer.Length] == '1';

            SetGroup(frontLEDs, state);
            SetGroup(backLEDs, state);
            SetGroup(leftLEDs, state);
            SetGroup(rightLEDs, state);
            return;
        }

        // ============================================================
        // 3) NORMAL PATTERN MODE
        // Her yüz kendi pattern'iyle çalýþýr.
        // backOnlyTest true ise sadece BACK pattern aktiftir.
        // ============================================================
        bool frontState = pFront[bitIndex % pFront.Length] == '1';
        bool backState = pBack[bitIndex % pBack.Length] == '1';
        bool leftState = pLeft[bitIndex % pLeft.Length] == '1';
        bool rightState = pRight[bitIndex % pRight.Length] == '1';

        if (backOnlyTest)
        {
            SetGroup(frontLEDs, false);
            SetGroup(backLEDs, backState);
            SetGroup(leftLEDs, false);
            SetGroup(rightLEDs, false);
        }
        else
        {
            SetGroup(frontLEDs, frontState);
            SetGroup(backLEDs, backState);
            SetGroup(leftLEDs, leftState);
            SetGroup(rightLEDs, rightState);
        }
    }

    void UpdateFramesPerBit()
    {
        if (targetFPS <= 0)
            targetFPS = 60;

        if (tickRate <= 0f)
            tickRate = 0.1f;

        framesPerBit = Mathf.Max(1, Mathf.RoundToInt(tickRate * targetFPS));
    }

    void SetGroup(GameObject[] leds, bool state)
    {
        if (leds == null)
            return;

        foreach (var led in leds)
        {
            if (led != null)
                led.SetActive(state);
        }
    }
}