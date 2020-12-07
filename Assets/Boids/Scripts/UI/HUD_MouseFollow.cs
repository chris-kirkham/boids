using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUD_MouseFollow : MonoBehaviour
{
    private Text mouseFollowText;

    // Start is called before the first frame update
    void Start()
    {
        mouseFollowText = GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        mouseFollowText.enabled = ControlInputs.Instance.useMouseFollow;
    }
}
