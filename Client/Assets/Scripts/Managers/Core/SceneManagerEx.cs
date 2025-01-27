using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerEx
{
    public BaseScene CurrentScene { get { return GameObject.FindObjectOfType<BaseScene>(); } }
    public void LoadScene(Define.Scene type)
    {
        Managers.Clear();

        Debug.Log($"{type.ToString()}을 불러옵니다.");
        LoadingSceneController.LoadScene(GetSceneName(type));
    }

    public void LoadMap(int mapId)
    {
        Managers.Clear();
        LoadingSceneController.LoadMap(mapId);
    }

    string GetSceneName(Define.Scene type)
    {
        string name = System.Enum.GetName(typeof(Define.Scene), type);
        return name;
    }

    public void Clear()
    {
        CurrentScene.Clear();
    }
}
