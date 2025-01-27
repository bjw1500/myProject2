using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Text.RegularExpressions;

//플레이어 슬롯은 캐릭터 모델링과 플레이어 정보를 담는다.
//Lobby는 그런 플레이어 슬롯을 관리한다.
//플레이어가 입장할 떄마다 슬롯 리스트에 추가한다.
//추가후에는 슬롯 설정을 해준다. 게임 캐릭터 생성 및 플레이어 정보

public class UI_Lobby : UI_Base
{
    public int MapId { get; set; } = 1;

    public Dictionary<int, UI_PlayerLobbySlot> _players = new Dictionary<int, UI_PlayerLobbySlot>();
    public UI_PlayerLobbySlot mySlot;
    [SerializeField] List<GameObject> charactersolots;
    [Header("캐릭터 선택 아이콘")]
    [SerializeField] GameObject characterSelectParent;

    [SerializeField] Sprite[] characterImg;     // <! 캐릭터 슬롯 이미지   
    //버튼
    //텍스트 연결
    enum Buttons
    {
        GameStart,
        NextMapButton,
        PreviousMapButton,
    }

    enum Images
    {
        MapImage,
    }

    Button _gameStart;
    Button _nextMapButton;
    Button _previousMapButton;
    [SerializeField] List<Button> _characterIcon;

    [SerializeField] public const int _mapCount = 4;
    Image _mapImage;
    [Header("맵")]
    public Sprite[] _imageSlot = new Sprite[_mapCount];

    private int extractInt = 0;

    public override void Init()
    {
        Bind<Button>(typeof(Buttons));
        Bind<Image>(typeof(Images));

        _gameStart = Get<Button>((int)Buttons.GameStart);
        _nextMapButton = Get<Button>((int)Buttons.NextMapButton);
        _previousMapButton = Get<Button>((int)Buttons.PreviousMapButton);
        _mapImage = Get<Image>((int)Images.MapImage);

        BindEvent(_gameStart.gameObject, GameStart, Define.UIEvent.Click);
        BindEvent(_nextMapButton.gameObject, NextMap, Define.UIEvent.Click);
        BindEvent(_previousMapButton.gameObject, PreviousMap, Define.UIEvent.Click);

        VrBindEvent(_gameStart.gameObject, GameStart);
        VrBindEvent(_nextMapButton.gameObject, NextMap);
        VrBindEvent(_previousMapButton.gameObject, PreviousMap);

        foreach (Transform child in characterSelectParent.transform)
            _characterIcon.Add(child.GetComponent<Button>());

        for (int i = 0; i < _characterIcon.Count; i++)
        {
            BindEvent(_characterIcon[i].gameObject, SelectedCharacter, Define.UIEvent.Click);
            VrBindEvent(_characterIcon[i].gameObject, SelectedCharacter);
        }

        GameObject userSlot = transform.Find("UserGrid").gameObject;
        foreach (Transform child in userSlot.transform)
        {
            child.gameObject.SetActive(true);      // <! 시작할 때 다 꺼주기
            charactersolots.Add(child.gameObject);
        }

        // slotInfo.Add(0, charactersolots[0]);        // <! 테스트 용
        _mapImage.sprite = _imageSlot[MapId];
    }

    public void NextMap(PointerEventData eventData) => NextMap();

    public void PreviousMap(PointerEventData eventData) => PreviousMap();

    public void GameStart(PointerEventData eventData) => GameStart();

    public void UpdateRoom(S_EnterWaitingRoom enterGamePacket)
    {
        //1.플레이어가 들어오면 슬롯 업데이트. 
        foreach (GameObject go in charactersolots)
        {
            //캐릭터 슬롯에는 PlayerLobbySlot 컴포넌트가 존재. 이곳에는 플레이어 정보가 담겨야 한다.
            //다만 플레이어 정보가 이미 담겨 있다면 다른 슬롯을 찾게 한다.
            UI_PlayerLobbySlot slot = go.GetComponent<UI_PlayerLobbySlot>();
            if (slot == null)
            {
                Debug.Log("UI_Lobby Error. UpdateRoom");
                return;
            }
            if (slot._playerInfo != null)
                continue;

            //여기까지 통과했으면 에러도 없고, 플레이어 정보도 없으니 업데이트 해주자.
            slot._playerInfo = enterGamePacket.Info.Player;
            slot._characterSelectNumber = enterGamePacket.Info.Player.ChracterId;
            go.GetComponent<Image>().sprite = characterImg[enterGamePacket.Info.Player.ChracterId];

            //내 슬롯인지 아닌지 구별.
            if (enterGamePacket.MyPlayer == true)
                mySlot = slot;

            //슬롯의 상태 업데이트.
            slot.Refresh();

            //로비 창 안에서 관리할 플레이어 목록 업데이트
            _players.Add(enterGamePacket.Info.ObjectId, slot);

            break;
        }
    }

    public void UpdateSlot(S_SelectCharacter packet)
    {
        UI_PlayerLobbySlot slot = null;
        _players.TryGetValue(packet.PlayerId, out slot);
        if (slot == null)
        {
            Debug.Log("Error UpdateSlot");
            return;
        }
        slot.transform.GetComponent<Image>().sprite = characterImg[packet.PlayerInfo.ChracterId];
        slot._playerInfo.ChracterId = packet.PlayerInfo.ChracterId;
    }

    public void LeaveGame(S_LeaveWaitingRoom leaveGamePacket)
    {
        UI_PlayerLobbySlot leavePlayer = null;
        if (_players.TryGetValue(leaveGamePacket.Info.ObjectId, out leavePlayer) == false)
        {
            Debug.Log("Lobby의 LeaveGame에 잘못된 ObjectId가 전달되었습니다.");
            return;
        }

        _players.Remove(leaveGamePacket.Info.ObjectId);
        Managers.Resource.Destroy(leavePlayer.gameObject);
    }

    public void LoadScene(int mapId)
    {
        //Managers.Scene.LoadScene(Define.Scene.Game);
        Managers.Scene.LoadMap(mapId);
    }

    public void SelectedCharacter(PointerEventData eventData)
    {
        extractInt = int.Parse(Regex.Replace(eventData.pointerClick.name, @"[^0-9]", ""));
        SelectedCharacter();
    }

    ///////////////////////////////////// VR /////////////////////////////////////
    public void GameStart()
    {
        C_StartGame start = new C_StartGame();

        //나중에 캐릭터 선택이 가능해질때를 위한 코드.
        foreach (UI_PlayerLobbySlot player in _players.Values)
        {
            start.Players.Add(player._playerInfo);
        }
        //나중에는 로비창에서 MapId 직접 고르게 하자.
        start.MapId = MapId;
        Managers.Network.Send(start);
    }

    public void NextMap()
    {
        //누르면 다음 맵으로 이동. 
        MapId++;
        if (MapId > _mapCount)
        {
            MapId = 1;
        }
        _mapImage.sprite = _imageSlot[MapId - 1];

        //맵 이름을 바꿔줘야 할까? 
    }

    public void PreviousMap()
    {
        MapId--;
        if (MapId < 1)
        {
            MapId = _mapCount;
        }
        _mapImage.sprite = _imageSlot[MapId - 1];
    }

    public void SelectedCharacter()
    {
        // slotInfo[0].GetComponent<Image>().sprite = characterImg[number]; // <! 테스트 용

        //캐릭터를 바꾸면 자신의 슬롯 이미지를 바꿔주자.
        //mySlot._characterSelectImage = characterImg[number];
        //mySlot._characterSelectNumber = number;

        //캐릭터를 선택했으면 정보를 뿌려줘야 한다.
        C_SelectCharacter select = new C_SelectCharacter();
        select.PlayerId = mySlot._playerInfo.ObjectId;
        /*캐릭터 선택할 때 0으로 강제 돼서 주석 처리후 수정*/
        select.CharacterNumber = GameMng.I.extractInt == 0 ? extractInt : GameMng.I.extractInt;
        // select.CharacterNumber = extractInt;
        Managers.Network.Send(select);
    }
}
