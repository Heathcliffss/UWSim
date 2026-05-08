using System.Collections;
using UnityEngine;

public class RovLeds : MonoBehaviour
{
    [Header("Iþýk Gruplarý")]
    public GameObject[] frontLEDs;
    public GameObject[] backLEDs;
    public GameObject[] leftLEDs;
    public GameObject[] rightLEDs;

  
    public bool isEmergency = false;

    [Header("Hýz Ayarý")]
    public float tickRate = 0.125f; // Her bir bitin süresi

    private string pFront = "11110000";
    private string pBack = "11001100";
    private string pLeft = "10101010";
    private string pRight = "10011001";
    private string pEmer = "1111111100000000";

    private int index = 0;

    void Start()
    {
        StartCoroutine(PatternLoop());
    }

    IEnumerator PatternLoop()
    {
        while (true)
        {
            if (isEmergency)
            {
                // Acil durum paterni için mod 16 kullanýyoruz
                bool state = pEmer[index % 16] == '1';
                SetGroup(frontLEDs, state);
                SetGroup(backLEDs, state);
                SetGroup(leftLEDs, state);
                SetGroup(rightLEDs, state);
            }
            else
            {
                // Normal paternler 8 bit olduðu için mod 8 kullanýyoruz
                int i8 = index % 8;
                SetGroup(frontLEDs, pFront[i8] == '1');
                SetGroup(backLEDs, pBack[i8] == '1');
                SetGroup(leftLEDs, pLeft[i8] == '1');
                SetGroup(rightLEDs, pRight[i8] == '1');
            }

            index++;
            if (index >= 16) index = 0; 

            yield return new WaitForSeconds(tickRate);
        }
    }

    void SetGroup(GameObject[] leds, bool state)
    {
        foreach (var led in leds)
        {
            if (led != null) led.SetActive(state);
        }
    }
}