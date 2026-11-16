using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestManager : MonoBehaviour
{
    public Button btn;

    public GameObject obj;
    public string key;
    [Button]
    public async UniTask CashingAsset()
    {
        obj = await AddressManager.GetAssetAsync<GameObject>(key);
    }

    [Button]
    public void ReleaseAsset()
    {
        obj = null;
        AddressManager.Release(key);
    }
}
