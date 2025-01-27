using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using UnityEngine.XR.Management;
using System;

public class GameMng : MonoBehaviour
{
    static private GameMng _instance;
    public static GameMng I
    {
        get
        {
            if (_instance.Equals(null))
            {
                // 여기 들어오면 안됨
                Debug.LogError("Instance is null");
            }
            return _instance;
        }
    }

    public Vr_Input input;
    public bool _isVR = false;
    public bool SingleGame = false;
    public Define.VRType VR_Type = Define.VRType.Vive;

    void Awake()
    {
        _instance = this;
        Managers.Init();
        DontDestroyOnLoad(this);
    }

    void Start()
    {
        _isVR = XRGeneralSettings.Instance.Manager.activeLoader;
        PhotonMng.ConnectToServer();
    }

    #region Action
    public CustomSnapTurn snapturn = null;

    public BulletPool bulletPool = null;
    #endregion

    #region UI
    public Hpbar hpbar;
    public int extractInt = 0;       // 캐릭터 아이콘 클릭 했을때
    #endregion

    #region Event
    // ---- LaserEvent
    public event PointerEventHandler PointerIn;
    public event PointerEventHandler PointerOut;
    public event PointerEventHandler PointerClick;

    public virtual void OnPointerIn(PointerEventArgs e)
    {
        if (PointerIn != null)
            PointerIn(this, e);
    }

    public virtual void OnPointerClick(PointerEventArgs e)
    {
        if (PointerClick != null)
            PointerClick(this, e);
    }

    public virtual void OnPointerOut(PointerEventArgs e)
    {
        if (PointerOut != null)
            PointerOut(this, e);
    }
    #endregion
}
