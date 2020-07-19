using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class HUD_FrameTime : MonoBehaviour
{
    public enum Mode { FrameRate, FrameTime} ;
    public Mode mode = Mode.FrameRate;
    private Text frameTimeText;

    // Start is called before the first frame update
    void Start()
    {
        frameTimeText = GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        if(mode == Mode.FrameRate)
        {
            frameTimeText.text = 1 / Time.deltaTime + " fps";
        }
        else
        {
            frameTimeText.text = Time.deltaTime * 1000 + " ms";
        }
    }
}
