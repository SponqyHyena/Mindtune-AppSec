using UnityEngine;
using DG.Tweening;
using System;


public class SwitchHandler : MonoBehaviour
{
    [SerializeField] private GameObject switchButton;
    private int switchState = 1;

    public void OnSwitchButtonClicked()
    {
        switchButton.transform.DOLocalMoveX(-switchButton.transform.localPosition.x, 0.05f);
        switchState = Math.Sign(-switchButton.transform.localPosition.x);
    }
}
