using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Threading.Tasks;
using UnityEngine.TextCore;

public class MagicWordsEmojiFetcher : MonoBehaviour
{
    [SerializeField] 
    private TMP_SpriteAsset m_DynamicSpriteAsset; //TMPro Sprite Atlas


    private static int s_usedSpaces = 0;
    private static List<string> s_savedEmojis = new List<string>();

    public bool IsEmojiAlreadyFetched(string emojiName) => s_savedEmojis.Contains(emojiName);
    private void EmojiSaved(string emojiName) => s_savedEmojis.Add(emojiName);
    
    
    public async Task DownloadAllEmojis(List<EmojiData> _data)
    {
        Debug.Log($"DownloadAllEmojis: {_data.Count}");
        foreach (var emojiData in _data)
        {
            if (IsEmojiAlreadyFetched(emojiData.name))
            {
                Debug.Log("Emoji already cached - avoiding download");
                continue;
            }
                
            Texture2D tex = await DownloadEmojiTexture(emojiData.url);
            if (tex != null)
            {
                AddEmojiToSpriteAsset(emojiData.name, tex);
            }
        }
    }
    private async Task<Texture2D> DownloadEmojiTexture(string url)
    {
        Debug.Log($"DownloadEmojiTexture: {url}");
        using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url))
        {
            try
            {
                await webRequest.SendWebRequest();
                return webRequest.result == UnityWebRequest.Result.Success
                    ? DownloadHandlerTexture.GetContent(webRequest)
                    : null;
            }
            catch
            {
                return null;
            }
        }
    }
    
    private Rect ReserveEmptySpace(Texture2D texture, int width, int height)
    {
        int x = s_usedSpaces * width % texture.width;
        int y = s_usedSpaces * width / texture.width;
        Rect emptySpace = new Rect(x, y, width, height);
        
        //Debug.Log($"Reserved empty space: {emptySpace}");
        s_usedSpaces += 1;
        return emptySpace;
    }
    
    private void AddEmojiToSpriteAsset(string emojiName, Texture2D newEmojiTexture)
    {
        Debug.Log($"AddEmojiToSpriteAsset: {emojiName}");
        
        // 1. Get references to the original texture
        Texture2D mainTexture = m_DynamicSpriteAsset.spriteSheet as Texture2D;
    
        Debug.Log($"AddEmojiToSpriteAsset2: {emojiName}");
        // 2. Create a temporary texture in RAM
        Texture2D workingTexture = new Texture2D(
            mainTexture.width, 
            mainTexture.height, 
            mainTexture.format, 
            false
        );
    
        Debug.Log($"AddEmojiToSpriteAsset3: {emojiName}");
        // 3. Copy original texture data
        Color[] originalPixels = mainTexture.GetPixels();
        workingTexture.SetPixels(originalPixels);
    
        Debug.Log($"AddEmojiToSpriteAsset4: {emojiName}");
        // 4. Find position and paste new emoji
        Rect emojiRect = ReserveEmptySpace(workingTexture, newEmojiTexture.width, newEmojiTexture.height);
        workingTexture.SetPixels(
            (int)emojiRect.x,
            (int)emojiRect.y,
            newEmojiTexture.width,
            newEmojiTexture.height,
            newEmojiTexture.GetPixels()
        );
        workingTexture.Apply();
    
        Debug.Log($"AddEmojiToSpriteAsset5: {emojiName}");
        // 5. Overwrite original texture using CopyTexture (most reliable method)
        Graphics.CopyTexture(workingTexture, mainTexture);
    
        Debug.Log($"AddEmojiToSpriteAsset6: {emojiName}");
        // 6. Create glyph data
        CreateGlyphData(emojiName, newEmojiTexture, emojiRect);
    
        Debug.Log($"AddEmojiToSpriteAsset7: {emojiName}");
        // 7. Cleanup
        Destroy(workingTexture);
        
        Debug.Log($"AddEmojiToSpriteAsset8: {emojiName}");
        // 8. Cache it
        EmojiSaved(emojiName);

    }
    
    
    private void CreateGlyphData(string emojiName, Texture2D emojiTexture, Rect emojiRect)
    {
        // Create glyph
        TMP_SpriteGlyph glyph = new TMP_SpriteGlyph
        {
            index = (uint)m_DynamicSpriteAsset.spriteGlyphTable.Count,
            metrics = new GlyphMetrics(
                emojiTexture.width,
                emojiTexture.height,
                0,
                emojiTexture.height,
                emojiTexture.width
            ),
            glyphRect = new GlyphRect(
                (int)emojiRect.x,
                (int)emojiRect.y,
                emojiTexture.width,
                emojiTexture.height
            ),
            sprite = Sprite.Create(
                (Texture2D)m_DynamicSpriteAsset.spriteSheet,
                new Rect(emojiRect.x, emojiRect.y, emojiTexture.width, emojiTexture.height),
                new Vector2(0.5f, 0.5f)
            )
        };

        // Create character
        TMP_SpriteCharacter character = new TMP_SpriteCharacter(
            (uint)(0xE000 + m_DynamicSpriteAsset.spriteCharacterTable.Count),
            glyph
        )
        {
            name = emojiName,
            scale = 1.0f
        };

        // Update asset
        m_DynamicSpriteAsset.spriteGlyphTable.Add(glyph);
        m_DynamicSpriteAsset.spriteCharacterTable.Add(character);
        m_DynamicSpriteAsset.UpdateLookupTables();
        TMPro_EventManager.ON_SPRITE_ASSET_PROPERTY_CHANGED(true, m_DynamicSpriteAsset);
    
        // Force refresh all TMP components using this asset
        foreach (var textComponent in Resources.FindObjectsOfTypeAll<TMP_Text>())
        {
            if (textComponent.spriteAsset == m_DynamicSpriteAsset)
            {
                textComponent.ForceMeshUpdate();
            }
        }
    }
}