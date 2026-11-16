using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class AddressManager
{
    class Cached
    {
        public AsyncOperationHandle Handle;
        public int Count;
    }

    class Loading
    {
        public int Count;
        public readonly UniTaskCompletionSource<object> Source = new();
    }

    static readonly Dictionary<string, Cached> cachedDic = new();
    static readonly Dictionary<string, Loading> loadingDic = new();

    public static async UniTask<T> GetAssetAsync<T>(string key)
    {
        if (cachedDic.TryGetValue(key, out Cached cached))
        {
            cached.Count++;
            return (T)cached.Handle.Result;
        }

        if (!loadingDic.TryGetValue(key, out Loading loading))
        {
            loading = new Loading { Count = 1 };
            loadingDic[key] = loading;
            LoadAsset<T>(key, loading).Forget();
        }
        else
        {
            loading.Count++;
        }

        object result = await loading.Source.Task;
        return (T)result;
    }

    static async UniTaskVoid LoadAsset<T>(string key, Loading loading)
    {
        AsyncOperationHandle<T> handle = default;

        try
        {
            handle = Addressables.LoadAssetAsync<T>(key);
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
                throw new System.Exception($"key : {key} 로드실패");

            if (loading.Count <= 0)
            {
                Addressables.Release(handle);
                handle = default;
                loading.Source.TrySetException(new System.Exception($"key : {key} 로드 중 모두 해제됨"));
                return;
            }

            var cached = new Cached
            {
                Handle = handle,
                Count = loading.Count
            };
            handle = default;
            cachedDic[key] = cached;
            loading.Source.TrySetResult(cached.Handle.Result);
        }
        catch (System.Exception e)
        {
            if (handle.IsValid())
                Addressables.Release(handle);

            loading.Source.TrySetException(e);
        }
        finally
        {
            loadingDic.Remove(key);
        }
    }

    public static void Release(string key)
    {
        if (cachedDic.TryGetValue(key, out Cached cached))
        {
            cached.Count--;
            if (cached.Count > 0)
                return;

            Addressables.Release(cached.Handle);
            cachedDic.Remove(key);
            return;
        }

        if (loadingDic.TryGetValue(key, out Loading loading) && loading.Count > 0)
            loading.Count--;
    }
}
