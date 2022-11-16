using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SniperAnmo : MonoBehaviour
{
    public GameObject[] bulletReady;
    public Sniper gun;

    private void Update()
    {
        if (gun.bulletcurCount == gun.ammoBulletCount)
        {
            for (int i = 0; i < bulletReady.Length; i++)
            {
                bulletReady[i].SetActive(true);
            }
        }

        else {
            for (int i = gun.ammoBulletCount - 1; i>=gun.bulletcurCount; i--) {
                bulletReady[i].SetActive(false);
            }
        }
    }

}
