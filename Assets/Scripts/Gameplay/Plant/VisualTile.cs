using System;
using Egglers;
using UnityEngine;
using UnityEngine.UI;

public class VisualTile : MonoBehaviour
{
    private Button button;
    public Vector2Int position;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        button = GetComponentInChildren<Button>();
        button.onClick.AddListener(OnButtonClick);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnButtonClick()
    {
        Debug.Log("Button Clicked");
        GridVisualizer.Instance.SetDisplayPlantBit(position);
    }

    void OnDestroy()
    {
        button.onClick.RemoveListener(OnButtonClick);
    }
}
