/*      
 * MIT License
 *
 * Copyright (c) 2025 maybekoi
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of
 * this software and associated documentation files (the "Software"), to deal in
 * the Software without restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the
 * Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN
 * AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH
 * THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

// I WILL NOT PROVIDE DOCUMENTATION FOR THIS SCRIPT BECAUSE IT WAS MADE FOR PERSONAL USE. YOU'RE WELCOME TO MAKE YOUR OWN DOCUMENTATION - koi

#if UNITY_SWITCH || UNITY_EDITOR
using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using nn.fs;
using FileSystem = nn.fs.FileSystem;
using SwitchFile = nn.fs.File;

public class SwitchStreamingAssetManager : MonoBehaviour
{
    private static SwitchStreamingAssetManager instance;
    public static SwitchStreamingAssetManager Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("SwitchStreamingAssetManager");
                instance = go.AddComponent<SwitchStreamingAssetManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private const string MOUNT_NAME = "streamingAssets";
    private bool isMounted = false;
    private Dictionary<string, byte[]> assetCache = new Dictionary<string, byte[]>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (isMounted)
        {
            try
            {
                FileSystem.Unmount(MOUNT_NAME);
                isMounted = false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SwitchStreamingAssetManager] Error unmounting: {ex.Message}");
            }
        }
    }

    private IEnumerator LoadAssetBundleInternal(string bundlePath, Action<AssetBundle> onSuccess, Action<string> onError)
    {
        if (!System.IO.File.Exists(bundlePath))
        {
            onError?.Invoke($"Asset bundle not found: {bundlePath}");
            yield break;
        }

        byte[] data;
        try
        {
            data = System.IO.File.ReadAllBytes(bundlePath);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SwitchStreamingAssetManager] Error reading asset bundle file: {ex.Message}");
            onError?.Invoke(ex.Message);
            yield break;
        }

        AssetBundleCreateRequest request = AssetBundle.LoadFromMemoryAsync(data);
        yield return new WaitWhile(() => !request.isDone);

        if (request.assetBundle == null)
        {
            onError?.Invoke("Failed to load asset bundle");
            yield break;
        }

        onSuccess?.Invoke(request.assetBundle);
    }

    public IEnumerator LoadAssetBundle(string bundleName, Action<AssetBundle> onSuccess, Action<string> onError)
    {
        string bundlePath = $"/rom/Data/StreamingAssets/{bundleName}";
        Debug.Log($"[SwitchStreamingAssetManager] Loading asset bundle: {bundlePath}");
        return LoadAssetBundleInternal(bundlePath, onSuccess, onError);
    }

    private IEnumerator LoadStreamingAssetInternal(string fullPath, Action<byte[]> onSuccess, Action<string> onError)
    {
        if (!System.IO.File.Exists(fullPath))
        {
            onError?.Invoke($"File not found: {fullPath}");
            yield break;
        }

        byte[] data;
        try
        {
            data = System.IO.File.ReadAllBytes(fullPath);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SwitchStreamingAssetManager] Error reading streaming asset file: {ex.Message}");
            onError?.Invoke(ex.Message);
            yield break;
        }

        onSuccess?.Invoke(data);
    }

    public IEnumerator LoadStreamingAsset(string path, Action<byte[]> onSuccess, Action<string> onError)
    {
        string fullPath = $"/rom/Data/StreamingAssets/{path}";
        Debug.Log($"[SwitchStreamingAssetManager] Loading streaming asset: {fullPath}");
        return LoadStreamingAssetInternal(fullPath, onSuccess, onError);
    }

    public IEnumerator LoadStreamingAssetAsText(string path, Action<string> onSuccess, Action<string> onError)
    {
        yield return LoadStreamingAsset(path, 
            (byte[] data) => 
            {
                try 
                {
                    string text = System.Text.Encoding.UTF8.GetString(data);
                    onSuccess?.Invoke(text);
                }
                catch (Exception ex)
                {
                    onError?.Invoke($"Failed to decode text: {ex.Message}");
                }
            },
            onError);
    }

    public void CacheAsset(string key, byte[] data)
    {
        if (assetCache.ContainsKey(key))
        {
            assetCache[key] = data;
        }
        else
        {
            assetCache.Add(key, data);
        }
    }

    public bool TryGetCachedAsset(string key, out byte[] data)
    {
        return assetCache.TryGetValue(key, out data);
    }

    public void ClearCache()
    {
        assetCache.Clear();
    }
}
#endif
