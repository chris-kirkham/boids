using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class HUD_FrameTime : MonoBehaviour
{
    public enum Mode { FrameRate, FrameTime} ;
    public Mode mode = Mode.FrameRate;
    [Min(0)] public float updateRate = 0.1f;
    private Text frameTimeText;

    void Start()
    {
        frameTimeText = GetComponent<Text>();
        StartCoroutine(UpdateFrameTimeText());
    }

    private IEnumerator UpdateFrameTimeText()
    {
        while(true)
        {
            if (mode == Mode.FrameRate)
            {
                frameTimeText.text = 1 / Time.deltaTime + " fps";
            }
            else
            {
                frameTimeText.text = Time.deltaTime * 1000 + " ms";
            }

            yield return new WaitForSeconds(updateRate);
        }
    }
}
