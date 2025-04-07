using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class DynamicSpritesCache
{
    private static readonly DynamicSpritesCache cachedInstance = new DynamicSpritesCache(cache: true);
    private static readonly DynamicSpritesCache uncachedInstance = new DynamicSpritesCache(cache: false);

    public static DynamicSpritesCache GetInstance(bool cached = true)
    {
#if !UNITY_WEBGL
        return cached ? cachedInstance : uncachedInstance;
#else
        return uncachedInstance;
#endif
    }

    private readonly bool cache;
    private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    private readonly Dictionary<string, List<Action<Sprite>>> callbacksByUrl = new Dictionary<string, List<Action<Sprite>>>();
    private int pendingDownloadCount = 0;
    private readonly object lockObject = new object();

    public event Action AllPendingDownloadsFinished;
    public event Action<string, Sprite> PendingDownloadFinished;

    public DynamicSpritesCache(bool cache = false)
    {
        this.cache = cache;
    }

    public void ClearEvents()
    {
        AllPendingDownloadsFinished = null;
        PendingDownloadFinished = null;
    }

    public bool ContainsKey(string key)
    {
        lock (lockObject)
        {
            return sprites.ContainsKey(key);
        }
    }

    public Sprite GetSprite(string key)
    {
        lock (lockObject)
        {
            return sprites.TryGetValue(key, out var sprite) ? sprite : null;
        }
    }

    public async Task<Sprite> DownloadSpriteAsync(string url, Action<Sprite> callback = null)
    {
        if (callback != null)
        {
            Subscribe(url, callback);
        }

        try
        {
            IncrementPendingCount();

            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            lock (lockObject)
            {
                if (sprites.TryGetValue(url, out var existingSprite))
                {
                    NotifySpriteReady(url, existingSprite);
                    return existingSprite;
                }
                sprites[url] = null; // Mark as pending
            }

            // Check cache first
            var cachedTexture = GetTexture(url);
            if (cachedTexture != null)
            {
                Debug.Log($"Texture already cached - avoiding download: ({url})");
                var sprite = CreateSprite(cachedTexture);
                lock (lockObject)
                {
                    sprites[url] = sprite;
                }
                NotifySpriteReady(url, sprite);
                return sprite;
            }

            // Download from web
            using (var request = UnityWebRequestTexture.GetTexture(url))
            {
                await request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    lock (lockObject)
                    {
                        sprites.Remove(url);
                    }
                    return null;
                }

                var texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                var newTexture = ToTexture2D(texture);
                
                Debug.Log($"DynamicSpritesCache - download completed ({url})");

#if !UNITY_WEBGL
                if (cache)
                {
                    SaveTexture(url, newTexture);
                }
#endif

                var newSprite = CreateSprite(newTexture);
                lock (lockObject)
                {
                    sprites[url] = newSprite;
                }
                NotifySpriteReady(url, newSprite);
                return newSprite;
            }
        }
        finally
        {
            DecrementPendingCount();
        }
    }

    public async Task DownloadAllSpritesAsync(IEnumerable<string> urls)
    {
        var tasks = new List<Task>();
        foreach (var url in urls)
        {
            tasks.Add(DownloadSpriteAsync(url));
        }
        await Task.WhenAll(tasks);
    }

    private void IncrementPendingCount()
    {
        lock (lockObject)
        {
            pendingDownloadCount++;
        }
    }

    private void DecrementPendingCount()
    {
        lock (lockObject)
        {
            pendingDownloadCount--;
            if (pendingDownloadCount == 0)
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() => AllPendingDownloadsFinished?.Invoke());
            }
        }
    }

    private void Subscribe(string url, Action<Sprite> callback)
    {
        if (string.IsNullOrEmpty(url)) return;

        lock (lockObject)
        {
            if (!callbacksByUrl.TryGetValue(url, out var callbacks))
            {
                callbacks = new List<Action<Sprite>>();
                callbacksByUrl[url] = callbacks;
            }

            if (!callbacks.Contains(callback))
            {
                callbacks.Add(callback);
            }
        }
    }

    public void RemoveSubscription(Action<Sprite> callback)
    {
        lock (lockObject)
        {
            foreach (var callbacks in callbacksByUrl.Values)
            {
                callbacks.Remove(callback);
            }
        }
    }

    private void NotifySpriteReady(string url, Sprite sprite)
    {
        UnityMainThreadDispatcher.Instance.Enqueue(() =>
        {
            PendingDownloadFinished?.Invoke(url, sprite);

            List<Action<Sprite>> callbacks;
            lock (lockObject)
            {
                if (callbacksByUrl.TryGetValue(url, out callbacks))
                {
                    callbacksByUrl.Remove(url);
                }
            }

            if (callbacks != null)
            {
                foreach (var callback in callbacks)
                {
                    try
                    {
                        callback?.Invoke(sprite);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Callback error for {url}: {e}");
                    }
                }
            }
        });
    }

    private Sprite CreateSprite(Texture2D texture)
    {
        if (texture == null) return null;

        return Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );
    }

    private void SaveTexture(string url, Texture2D texture)
    {
        try
        {
            byte[] bytes = url.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
                          url.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                ? texture.EncodeToJPG()
                : texture.EncodeToPNG();

            PlayerPrefs.SetString(url, Convert.ToBase64String(bytes));
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save texture {url}: {e}");
        }
    }

    private Texture2D GetTexture(string url)
    {
        string base64 = PlayerPrefs.GetString(url, "");
        if (string.IsNullOrEmpty(base64)) return null;

        try
        {
            byte[] bytes = Convert.FromBase64String(base64);
            var texture = new Texture2D(2, 2);
            texture.LoadImage(bytes);
            return texture;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load cached texture {url}: {e}");
            return null;
        }
    }

    private static Texture2D ToTexture2D(Texture texture)
    {
        if (texture is Texture2D tex2D) return tex2D;

        return Texture2D.CreateExternalTexture(
            texture.width,
            texture.height,
            TextureFormat.RGBA32,
            false, false,
            texture.GetNativeTexturePtr());
    }
}