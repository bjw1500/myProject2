using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;
using Photon.Pun;

public class ObjectManager
{
    public CharacterMainControllerVR MyPlayer { get; set; }

    public Dictionary<int, ObjectInfo> _players = new Dictionary<int, ObjectInfo>();
    public Dictionary<int, GameObject> _objects = new Dictionary<int, GameObject>();

    public Dictionary<int, Item> _items = new Dictionary<int, Item>();
    public UI_PlayerList PlayerList { get; set; }

    private int ItemIdCount = 0;

    //나중에 시작옵션에서 타입 바꿀 수 있도록 수정하기.
    public VRType VR_Type = VRType.Vive;

    public int _mapId = 0;
    public PlayerInfo _singlePlayer;

    public void Add(ObjectInfo info, bool myPlayer = false)
    {
        if (info == null)
            return;

        if (myPlayer == true)
        {
            int SelectedCharacter = info.Player.ChracterId;
            GameObject go = Managers.Resource.Instantiate($"Player/Player_{SelectedCharacter}");

            if (go == null)
                Debug.Log("캐릭터가 존재하지 않습니다.");

            go.name = info.Player.Name;
            _objects.Add(info.ObjectId, go);
            MyPlayer = go.GetOrAddComponent<CharacterMainControllerVR>();
            MyPlayer.Info = info;
            MyPlayer.enabled = true;

            go.transform.position = new Vector3(info.MovementInfo.PlayerPosInfo.PosX, info.MovementInfo.PlayerPosInfo.PosY, info.MovementInfo.PlayerPosInfo.PosZ);
        }
        else
        {
            int SelectedCharacter = info.Player.ChracterId;
            GameObject go = Managers.Resource.Instantiate($"Player/RemotePlayer_{SelectedCharacter}");

            go.name = info.Player.Name;
            _objects.Add(info.ObjectId, go);
            CharacterRemoteControllerVR controller = go.GetOrAddComponent<CharacterRemoteControllerVR>();
            controller.Info = info;
            controller.enabled = true;

            go.transform.position = new Vector3(info.MovementInfo.PlayerPosInfo.PosX, info.MovementInfo.PlayerPosInfo.PosY, info.MovementInfo.PlayerPosInfo.PosZ);
            Debug.Log($"{info.Player.Name} - {info.ObjectId}가 생성되었습니다.");
        }

        _players.Add(info.ObjectId, info);


        if (PlayerList == null)
            return;

        PlayerList.SetList();

    }

    public void AddItem(ItemInfo info)
    {
        ItemData data = null;
        Managers.Data.ItemDict.TryGetValue(info.Name, out data);
        GameObject go = Managers.Resource.Instantiate(data.prefabPath);
        go.name = info.Name;
        go.transform.position = new Vector3(info.Position.PosX, info.Position.PosY, info.Position.PosZ);

        Item item = null;
        if (info.ItemType == ItemType.Weapon)
        {
            item = go.GetComponent<Weapon>();


        }
        else if (info.ItemType == ItemType.Armor)
        {
            item = go.GetComponent<Armor>();


        }
        else if (info.ItemType == ItemType.Consumable)
        {
            item = go.GetComponent<Consumable>();


        }

        item.ItemData = data;
        item.Info = info;
        item.IconImage = Managers.Resource.Load<Sprite>(data.imagePath);

        //무기 정보 연동해주기.


        _items.Add(info.ObjectId, item);

        Debug.Log($"{info.Name} - {info.ObjectId}가 생성되었습니다.");
    }

    public void AddObject(ObjectInfo info)
    {
        GameObject go = Managers.Resource.Instantiate($"Object/{info.StatInfo.Name}");
        go.name = info.Name;
        go.transform.position = new Vector3(info.MovementInfo.PlayerPosInfo.PosX, info.MovementInfo.PlayerPosInfo.PosY, info.MovementInfo.PlayerPosInfo.PosZ);

        //TODO 
        //나중에 ObjectController 만들어주기
        BaseController bc = go.GetComponent<HitBox>();
        bc.Info = info;
        _objects.Add(info.ObjectId, go);

        Debug.Log($"{info.Name} - {info.ObjectId}가 생성되었습니다.");
    }

    public void AddSingleGame(ObjectInfo info, bool isVR = false)
    {
        GameObject go = Managers.Resource.Instantiate($"Player/Player_{_singlePlayer.ChracterId}");

        go.name = info.Name;
        _objects.Add(info.ObjectId, go);
        MyPlayer = go.GetComponent<CharacterMainControllerVR>();
        MyPlayer.Info = info;
        MyPlayer.enabled = true;


        StatInfo stat = null;

        Managers.Data.StatDict.TryGetValue("Player", out stat);
        MyPlayer.Stat = stat;

        go.transform.position = new Vector3(info.MovementInfo.PlayerPosInfo.PosX, info.MovementInfo.PlayerPosInfo.PosZ, info.MovementInfo.PlayerPosInfo.PosY);
    }

    public void UpdatePlayerInfo(ObjectInfo info)
    {
        ObjectInfo player = null;
        if (_players.TryGetValue(info.ObjectId, out player) == false)
        {
            Debug.Log("UpdatePlayerInfo 에러");
            return;
        }
        player.Player = info.Player;
        PlayerList.RefreshUI();
    }


    public void Remove(int Id)
    {
        GameObject go = null;
        if (_objects.TryGetValue(Id, out go) == false)
            return;

        if (_players.ContainsKey(Id) == true)
        {
            _players.Remove(Id);
        }


        _objects.Remove(Id);
        Managers.Resource.Destroy(go);
    }

    public void RemoveMyPlayer()
    {
        _objects.Remove(MyPlayer.Info.ObjectId);
        _players.Remove(MyPlayer.Info.ObjectId);
        Managers.Resource.Destroy(MyPlayer.gameObject);
    }

    public GameObject Find(int Id)
    {
        GameObject target = null;
        _objects.TryGetValue(Id, out target);
        return target;
    }

    public GameObject Find(Func<GameObject, bool> condition)
    {
        foreach (GameObject obj in _objects.Values)
        {
            if (condition.Invoke(obj))
                return obj;
        }

        return null;
    }

    public void Clear()
    {
        _objects.Clear();
    }
}
