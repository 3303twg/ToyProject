using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class HotUpdateLoader : MonoBehaviour
{
    const string Address = "HotUpdate";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void StartLoad()
    {
        var runner = new GameObject(nameof(HotUpdateLoader));
        Object.DontDestroyOnLoad(runner);
        runner.AddComponent<HotUpdateLoader>();
    }

    async void Start()
    {
#if UNITY_EDITOR
        Attach(typeof(HotSwap));
        // #else
#else
/*
        //리모트 어드레서블 교체
        var init = Addressables.InitializeAsync();
                await init.Task;

                var check = Addressables.CheckForCatalogUpdates(false);
                await check.Task;
                if (check.Status == AsyncOperationStatus.Succeeded && check.Result != null && check.Result.Count > 0)
                {
                    var update = Addressables.UpdateCatalogs(check.Result, false);
                    await update.Task;
                }

                var handle = Addressables.LoadAssetAsync<TextAsset>(Address);
                await handle.Task;
                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError(handle.OperationException);
                    return;
                }

                var assembly = Assembly.Load(handle.Result.bytes);
                Attach(assembly.GetType("HotSwap"));

                */

                //로컬 파일 교체
        var path = Path.Combine(Application.streamingAssetsPath, "HotUpdate.dll.bytes");
        var assembly = Assembly.Load(File.ReadAllBytes(path));
        Attach(assembly.GetType("HotSwap"));
#endif
    }

    static void Attach(System.Type hotSwap)
    {
        var target = GameObject.Find("HybirdCLR");
        if (target != null && hotSwap != null && target.GetComponent(hotSwap) == null)
            target.AddComponent(hotSwap);
    }
}
