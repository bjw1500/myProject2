using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using static Define;

public class CharacterMainControllerVR : BaseController
{
    [Serializable]
    public class CharacterState
    {
        public bool isCurrentFp;
        public bool isMoving;
        public bool isRunning;
        public bool isGrounded;
        public bool isCursorActive;
    }


    public CharacterState State => _state;

    [SerializeField]
    VRType Type = VRType.Vive;


    [Space, SerializeField] private CharacterState _state = new CharacterState();

    private float _deltaTime;
    [SerializeField]
    private float _distFromGround;
    // Animation Params
    private float _moveX;
    private float _moveZ;

    [SerializeField]
    private Vector3 _moveDir;

    /// <summary> 월드 이동 벡터 </summary>
    [SerializeField]
    private Vector3 _worldMoveDir = Vector3.zero;
    public Vector3 WorldMoveDir
    {
        get { return _worldMoveDir; }
        set
        {
            Info.MovementInfo.PlayerPosInfo.PosX = value.x;
            Info.MovementInfo.PlayerPosInfo.PosY = value.y;
            Info.MovementInfo.PlayerPosInfo.PosZ = value.z;
            _worldMoveDir = value;
        }
    }

    //캐릭터 스탯

    public override int Hp
    {
        get { return Info.StatInfo.Hp; }
        set
        {
            Info.StatInfo.Hp = value;

            if (Com.hpbar != null)
                Com.hpbar.changeHp = Info.StatInfo.Hp;
        }

    }

    //무기 관련 필드
    public int _weaponSlotSize = 4;
    [SerializeField]
    public int _currentWeaponSlot = 0;

    bool _autoFire = false;

    private void Update()
    {
        _deltaTime = Time.deltaTime;

        SetTriggerInput();
        SetValuesByInput();
        // 3. Updates
        CheckGroundDistance();
        UpdateAnimationParams();
        UpdatePosition();
    }

    public override void Init()
    {
        base.Init();

        InitComponents();
        InitSettings();
    }


    private void InitComponents()
    {
        Com.myGun = GetComponentInChildren<WeaponController>();
        Com.cam = GetComponentInChildren<Camera>();
        Com.anim = GetComponentInChildren<Animator>();

        Com.CameraRig = Util.FindChild(transform.gameObject, "CameraRig");
        Com.RightController = Util.FindChild(Com.CameraRig, "RightController");
        Com.LeftController = Util.FindChild(Com.CameraRig, "LeftController");

        _weaponSlotSize = 4;
        Com.weaponSlot = new GameObject[_weaponSlotSize];

        TryGetComponent(out Com.movement3D);
        if (Com.movement3D == null)
            Com.movement3D = gameObject.AddComponent<PhysicsBasedMovement>();


        Com.hpbar = gameObject.GetComponentInChildren<Hpbar>();
        Com.bulletcount = gameObject.GetComponentInChildren<BulletCount>();
        Com.bulletcount.controller = this;

    }

    private void InitSettings()
    {
        Type = GameMng.I.VR_Type;
    }

    /***********************************************************************
    *                               Check Methods
    ***********************************************************************/
    #region .
    private void LogNotInitializedComponentError<T>(T component, string componentName) where T : Component
    {
        if (component == null)
            Debug.LogError($"{componentName} 컴포넌트를 인스펙터에 넣어주세요");
    }

    #endregion
    /***********************************************************************
    *                               Methods
    ***********************************************************************/


    private void SetTriggerInput()
    {

        if (GameMng.I.input.getStateUpFireTrigger)
        {
            _autoFire = false;
        }
        else if (GameMng.I.input.getStateFireTrigger)
        {
            _autoFire = true;

        }

        if (_autoFire == true && Com.myGun != null)
        {

            if (Com.myGun._weaponData.IsThrowable == true)
                return;

            Debug.Log("Fire");
            C_Skill c_Skill = new C_Skill();
            c_Skill.Info = Info;
            c_Skill.Skillid = 1;
            Managers.Network.Send(c_Skill);
            Com.myGun.Fire();
        }

        if(GameMng.I.input.getStategrabGrip && Com.myGun != null && Com.myGun._weaponData.IsThrowable == true)
        {
            Granade granade = Com.myGun as Granade;

            if (granade.pullPin == false)
            {

                Debug.Log("Fire");
                C_Skill c_Skill = new C_Skill();
                c_Skill.Info = Info;
                c_Skill.Skillid = 1;
                Managers.Network.Send(c_Skill);
                Com.myGun.Fire();
            }
            else
            {
                PickUp Granade = Com.RightController.GetComponentInChildren<PickUp>();
                if (Granade == null)
                    return;
                Granade.Drop();
            }
        }

        // Jump
        if (GameMng.I.input.getStateJumpBtn)
        {
            C_Skill c_Skill = new C_Skill();
            c_Skill.Info = Info;
            c_Skill.Skillid = 2;
            Managers.Network.Send(c_Skill);
            Jump();
        }



    }

    private void SetValuesByInput()
    {
        float h = 0f, v = 0f;

        //오큘러스 or Vive
        if (Type == VRType.Oculus)
        {
            // Debug.Log(Com.touchPosition.GetAxis(Com.left_hand));
            h = GameMng.I.input.getMoveAxis.x;
            v = GameMng.I.input.getMoveAxis.y;
        }
        else if (Type == VRType.Vive)
        {
            //  Debug.Log(Com.touchPosition.GetAxis(Com.left_hand));
            h = GameMng.I.input.getMoveAxis.x;
            v = GameMng.I.input.getMoveAxis.y;
        }

        // Move, Rotate
        SendMoveInfo(h, v);

        //입력값이 없으면 움직임 X
        State.isMoving = h != 0 || v != 0;

        //TODO그립 기능 추가되면 달리기 추가하기.
        //State.isRunning = Input.GetKey(Key.run);
    }

    private void UpdateAnimationParams()
    {
        float x, z;

        x = _moveDir.x;
        z = _moveDir.z;

        if (State.isRunning)
        {
            x *= 2f;
            z *= 2f;
        }

        // 보간
        const float LerpSpeed = 0.05f;
        _moveX = Mathf.Lerp(_moveX, x, LerpSpeed);
        _moveZ = Mathf.Lerp(_moveZ, z, LerpSpeed);

        Com.anim.SetFloat(GameMng.I.input.paramMoveX, _moveX);
        Com.anim.SetFloat(GameMng.I.input.paramMoveZ, _moveZ);
        Com.anim.SetFloat(GameMng.I.input.paramDistY, _distFromGround);
        Com.anim.SetBool(GameMng.I.input.paramGrounded, State.isGrounded);
    }

    /***********************************************************************
    *                               Movement Methods
    ***********************************************************************/
    #region .
    /// <summary> 땅으로부터의 거리 체크 - 애니메이터 전달용 </summary>
    private void CheckGroundDistance()
    {
        _distFromGround = Com.movement3D.GetDistanceFromGround();
        State.isGrounded = Com.movement3D.IsGrounded();
        Info.MovementInfo.Ground = State.isGrounded;
    }

    private void SendMoveInfo(float horizontal, float vertical)
    {
        _moveDir = new Vector3(horizontal, 0f, vertical).normalized;
        WorldMoveDir = Com.cam.transform.TransformDirection(_moveDir);
        Com.movement3D.SetMovement(WorldMoveDir, State.isRunning);
        Info.MovementInfo.Running = State.isRunning;
    }

    private void Jump()
    {
        bool jumpSucceeded = Com.movement3D.SetJump();

        if (jumpSucceeded)
        {
            // 애니메이션 점프 트리거
            Com.anim.SetTrigger(GameMng.I.input.paramJump);
        }
    }

    public void GetWeapon(Item item)
    {

        Weapon weapon = item as Weapon;
        Debug.Log($"{Info.Name}이 {item.Name}를 획득 했습니다.");


        Rigidbody body = item.transform.gameObject.GetComponent<Rigidbody>();
        Collider col = item.transform.GetComponent<Collider>();

        //수류탄을 던지기 위해서는 RigidBody가 활성화 되어 있어야 한다.
        body.isKinematic = true;
        body.useGravity = false;
        col.enabled = false;

        //슬롯을 채워주는 동시에 현재 플레이어의 무기를 바꿔준다.
        for (int i = 0; i < _weaponSlotSize; i++)
        {
            if (Com.weaponSlot[i] == null)
            {
                //무기 슬롯이 비어있다면, 채워주고 
                Com.weaponSlot[i] = item.transform.gameObject;

                //현재 사용중인 무기를 비활성화 해준다음, 주운 무기로 바꿔준다.
                if (Com.myGun != null)
                {
                    Com.myGun.gameObject.SetActive(false);
                }

                _currentWeaponSlot = i;
                item.transform.SetParent(Com.RightController.transform, false);


                Com.myGun = item.transform.GetComponent<WeaponController>();
                Com.myGun._master = this;
                Com.bulletcount.weaponController = Com.myGun;
                break;

            }
            else if (i == _weaponSlotSize - 1)
            {
                //무기 슬롯이 비어 있지 않다면,
                //현재 장착 중인 무기를 버리고 교환한다.
                Com.weaponSlot[_currentWeaponSlot] = null;
                Managers.Resource.Destroy(Com.myGun.gameObject);

                _currentWeaponSlot = i;
                item.transform.SetParent(Com.RightController.transform, false);


                Com.weaponSlot[_currentWeaponSlot] = item.transform.gameObject;
                Com.myGun = item.transform.GetComponent<WeaponController>();
                Com.myGun._master = this;
                break;
            }
        }

        //무기를 주웠을 때 위치 초기화 해주기.

        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;


        //서버 부분
        if (GameMng.I.SingleGame == true)
            return;

        item.Info.Master = Info;

        C_GetWeapon getWeapon = new C_GetWeapon();
        getWeapon.PlayerId = Info.ObjectId;
        getWeapon.Info = item.Info;
        getWeapon.Slot = _currentWeaponSlot;
        Managers.Network.Send(getWeapon);


        //TODO
        //무기 교체하는 패킷 보내기.
        /*
         * 원격으로 아이템을 먹으려면 어떻게 해야할까.
         * 
         * RemotePlayer가 먹어야할 아이템을 알고 있어야 한다.
         
         * 그런데 RemotePlayer가 먹어야할 아이템을 알려면 아이템 ID가 필요할 것이다.
         * 
         * 싱글 게임에서는 굳이 ID 발급이 필요 없겠지만,
         * 
         * Id발급이 필요하면 서버에서 발급해줘야 하는데...?
         * 
         *1.아이템 생성기를 만든다.
         *      Single 게임에서는 그냥 생성
         *      멀티 모드일 때는 서버 연동해서 생성.
         *
         *2.아이템 스폰 지점을 설정한다.
         *      스폰 지점은 게임 오브젝트 생성 후 ITemSpawnPoin Script를 붙인다.
         *
         *3.스폰 지점에서 일정 반경 주변으로 아이템이 나오게 만든다.
         *
         *
         *4.서버에서 아이템을 생성 후 Id를 부여해서 플레이어들한테 뿌린다.
         */


    }

    public void ChangeWeapon(int slot)
    {
        //현재 무기 숨기기
        WeaponController currentWeapon = Com.weaponSlot[_currentWeaponSlot].GetComponent<WeaponController>();
        currentWeapon.gameObject.SetActive(false);

        //무기 변경
        _currentWeaponSlot = slot;
        WeaponController wc = Com.weaponSlot[slot].GetComponent<WeaponController>();
        wc.gameObject.SetActive(true);
        wc._master = this;
        Com.myGun = wc;
        Com.bulletcount.weaponController = wc;


        //서버 부분
        if (GameMng.I.SingleGame == true)
            return;

        //무기 슬롯 번호 보내기
        //플레이어 id 보내기.
        C_ChangeWeapon changeWeapon = new C_ChangeWeapon();
        changeWeapon.PlayerId = Info.ObjectId;
        changeWeapon.Slot = slot;
        Managers.Network.Send(changeWeapon);


    }

    public override void OnDead(ObjectInfo attacker)
    {
        base.OnDead(attacker);

        //죽으면 리스폰 하게 만들기.
        //무기는 처음부터 다시.
        //TODO
        //RC에도 죽음 판정 만들어주기
        Debug.Log($"{attacker.Name}에 의해 {Info.Name}이 파괴됩니다.");
        Managers.Object.RemoveMyPlayer();
    }

    #endregion

    /***********************************************************************
    *                               Public Methods
    ***********************************************************************/
    #region .
    public void KnockBack(in Vector3 force, float time)
    {
        Com.movement3D.KnockBack(force, time);
    }

    #endregion



    //서버 관련 함수들
    public void UpdateServerPosition()
    {
        C_Move movePacket = new C_Move();
        movePacket.MovementInfo = Info.MovementInfo;
        Managers.Network.Send(movePacket);
    }

    public void UpdatePosition()
    {
        if (GameMng.I.SingleGame == true)
            return;

        #region 위치 업데이트

        //이게 최선인가? 좀 더 개선해볼것.
        Info.MovementInfo.MoveDir.PosX = _moveDir.x;
        Info.MovementInfo.MoveDir.PosY = _moveDir.y;
        Info.MovementInfo.MoveDir.PosZ = _moveDir.z;

        Info.MovementInfo.PlayerPosInfo.PosX = transform.position.x;
        Info.MovementInfo.PlayerPosInfo.PosY = transform.position.y;
        Info.MovementInfo.PlayerPosInfo.PosZ = transform.position.z;

        Info.MovementInfo.PlayerPosInfo.RotateX = transform.rotation.eulerAngles.x;
        Info.MovementInfo.PlayerPosInfo.RotateY = transform.rotation.eulerAngles.y;
        Info.MovementInfo.PlayerPosInfo.RotateZ = transform.rotation.eulerAngles.z;

        Info.MovementInfo.CameraPosInfo.PosX = Com.cam.transform.position.x;
        Info.MovementInfo.CameraPosInfo.PosY = Com.cam.transform.position.y;
        Info.MovementInfo.CameraPosInfo.PosZ = Com.cam.transform.position.z;

        Info.MovementInfo.CameraPosInfo.RotateX = Com.cam.transform.rotation.eulerAngles.x;
        Info.MovementInfo.CameraPosInfo.RotateY = Com.cam.transform.rotation.eulerAngles.y;
        Info.MovementInfo.CameraPosInfo.RotateZ = Com.cam.transform.rotation.eulerAngles.z;

        Info.MovementInfo.RightHandPosInfo.PosX = Com.RightController.transform.position.x;
        Info.MovementInfo.RightHandPosInfo.PosY = Com.RightController.transform.position.y;
        Info.MovementInfo.RightHandPosInfo.PosZ = Com.RightController.transform.position.z;

        Info.MovementInfo.RightHandPosInfo.RotateX = Com.RightController.transform.rotation.eulerAngles.x;
        Info.MovementInfo.RightHandPosInfo.RotateY = Com.RightController.transform.rotation.eulerAngles.y;
        Info.MovementInfo.RightHandPosInfo.RotateZ = Com.RightController.transform.rotation.eulerAngles.z;

        Info.MovementInfo.LeftHandPosInfo.PosX = Com.LeftController.transform.position.x;
        Info.MovementInfo.LeftHandPosInfo.PosY = Com.LeftController.transform.position.y;
        Info.MovementInfo.LeftHandPosInfo.PosZ = Com.LeftController.transform.position.z;

        Info.MovementInfo.LeftHandPosInfo.RotateX = Com.LeftController.transform.rotation.eulerAngles.x;
        Info.MovementInfo.LeftHandPosInfo.RotateY = Com.LeftController.transform.rotation.eulerAngles.y;
        Info.MovementInfo.LeftHandPosInfo.RotateZ = Com.LeftController.transform.rotation.eulerAngles.z;

        #endregion
        UpdateServerPosition();
    }


}
