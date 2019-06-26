using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Microsoft.MixedReality.Toolkit.UI;

public class ShowSliderValue : MonoBehaviour
{
    [SerializeField]
    private TextMeshPro textMeshPro = null;
    private void Start()
    {
        if (textMeshPro == null)
        {
            textMeshPro = GetComponent<TextMeshPro>();
        }
    }
    public void OnSliderUpdated(SliderEventData eventData)
    {
        if (textMeshPro != null)
        {
            textMeshPro.text = $": {eventData.NewValue}";
        }
    }
}
