using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

public class MagicWords : MonoBehaviour
{
    [SerializeField] 
    private MagicWordsEmojiFetcher m_emojiFetcher; //emoji cache - updates TMPro Atlas with emojis
    private DynamicSpritesCache m_dynamicSpritesCache => DynamicSpritesCache.GetInstance(cached: true); // avatar cache
    
    private DialogueData m_data;
        
    [Header("Dialogue UI")]
    [SerializeField] private GameObject m_LoadingBlocker;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Image leftAvatar, rightAvatar;
    

    private void Start()
    {
        FetchDialogueData();
    }

    private async void FetchDialogueData()
    {
        string url = "https://private-624120-softgamesassignment.apiary-mock.com/v2/magicwords";
        Debug.Log($"Fetching dialogue from: {url}");
    
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            await webRequest.SendWebRequest();
        
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    // Manually parse to handle the "dialogue"/"emojies" spelling
                    var jsonData = JsonUtility.FromJson<DialogueData>(webRequest.downloadHandler.text);
                    Debug.Log($"Successfully loaded {jsonData.dialogue.Count} dialogue lines");
                    Debug.Log($"Found {jsonData.emojies.Count} emojis and {jsonData.avatars.Count} avatars");
                
                    await ProcessDialogueData(jsonData);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to parse dialogue data: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"Failed to fetch dialogue: {webRequest.error}");
            }
        }
    }


    private async Task ProcessDialogueData(DialogueData _data)
    {
        m_data = _data;
        m_data.InitializeDictionaries();
        
        // Download & Cache avatars and emojis if not cached
        await m_dynamicSpritesCache.DownloadAllSpritesAsync(m_data.avatars.Select(a => a.url));
        await m_emojiFetcher.DownloadAllEmojis(m_data.emojies);
        
        StartDialogue();
    }
    
    public async void StartDialogue()
    {
        if (m_data == null || m_data.dialogue == null || m_data.dialogue.Count == 0)
        {
            Debug.LogError("No dialogue data available!");
            return;
        }

        m_LoadingBlocker.SetActive(false);
        dialogueText.text = string.Empty;

        foreach (var line in m_data.dialogue)
        {
            // Format: "[name]: [sentence]"
            string processedText = $"{line.name}: {ProcessEmojis(line.text)}";
            UpdateAvatarDisplay(line.name);
            await TypeText(processedText);
            await Task.Delay(2000);
        }
    }

    private void UpdateAvatarDisplay(string speakerName)
    {
        var avatar = m_data.GetAvatar(speakerName);
        if (avatar == null) return;

        // Get the sprite from cache
        Sprite avatarSprite = m_dynamicSpritesCache.GetSprite(avatar.url);
        if (avatarSprite == null) return;

        // Display in correct position
        if (avatar.IsLeftPosition)
        {
            leftAvatar.sprite = avatarSprite;
            leftAvatar.gameObject.SetActive(true);
            rightAvatar.gameObject.SetActive(false);
        }
        else
        {
            rightAvatar.sprite = avatarSprite;
            rightAvatar.gameObject.SetActive(true);
            leftAvatar.gameObject.SetActive(false);
        }
    }
    

    private string ProcessEmojis(string text)
    {
        // Replace {emojiname} with <sprite name="emojiname">
        return Regex.Replace(text, @"\{(\w+)\}", match => 
        {
            string emojiName = match.Groups[1].Value;
            if (m_emojiFetcher.IsEmojiAlreadyFetched(emojiName))
                return $"<sprite name=\"{emojiName}\">";
            else
                return $"";
        });
    }

    private async Task TypeText(string text)
    {
        dialogueText.text = string.Empty;
        int i = 0;
    
        while (i < text.Length)
        {
            // Check for sprite tag opening
            if (text[i] == '<' && text.Substring(i).StartsWith("<sprite"))
            {
                int endIndex = text.IndexOf('>', i);
                if (endIndex > i)
                {
                    // Extract and add complete sprite tag
                    string spriteTag = text.Substring(i, endIndex - i + 1);
                    dialogueText.text += spriteTag;
                    i = endIndex + 1;
                
                    // Pause for emoji display
                    await Task.Delay(50);
                    continue;
                }
            }
        
            // Handle normal characters and rich text tags
            dialogueText.text += text[i];
            i++;
        
            // Skip typing delay for rich text tags
            if (text[i-1] == '<')
            {
                // Fast-forward through the entire tag
                int tagEnd = text.IndexOf('>', i-1);
                if (tagEnd > i-1)
                {
                    string tagContent = text.Substring(i, tagEnd - i);
                    dialogueText.text += tagContent + ">";
                    i = tagEnd + 1;
                }
            }
            else
            {
                await Task.Delay(50);
            }
        }
    }
    
    
}


[System.Serializable]
public class DialogueData
{
    public List<DialogueLine> dialogue;
    public List<EmojiData> emojies;
    public List<AvatarData> avatars;
    
    // Runtime dictionaries for fast access
    [NonSerialized] private Dictionary<string, DialogueLine> _dialogueDict;
    [NonSerialized] private Dictionary<string, EmojiData> _emojiesDict;
    [NonSerialized] private Dictionary<string, AvatarData> _avatarsDict;
    [NonSerialized] private bool _initialized;
    
    
    public void InitializeDictionaries()
    {
        if (_initialized) return;
        
        _dialogueDict = new Dictionary<string, DialogueLine>();
        foreach (var line in dialogue)
        {
            _dialogueDict[line.name] = line;
        }
        
        _emojiesDict = new Dictionary<string, EmojiData>();
        foreach (var emoji in emojies)
        {
            _emojiesDict[emoji.name] = emoji;
        }
        
        _avatarsDict = new Dictionary<string, AvatarData>();
        foreach (var avatar in avatars)
        {
            _avatarsDict[avatar.name] = avatar;
        }
        
        _initialized = true;
    }

    // Fast access properties
    public DialogueLine GetDialogueLine(string name) => _dialogueDict.TryGetValue(name, out var line) ? line : null;
    public EmojiData GetEmoji(string name) => _emojiesDict.TryGetValue(name, out var emoji) ? emoji : null;
    public AvatarData GetAvatar(string name) => _avatarsDict.TryGetValue(name, out var avatar) ? avatar : null;
}

[System.Serializable]
public class DialogueLine
{
    public string name;
    public string text;
    
    public override string ToString() => $"{name}: {text}";
}

[System.Serializable]
public class EmojiData
{
    public string name;
    public string url;
    
    public override string ToString() => $"{name}: {url}";
}

[System.Serializable]
public class AvatarData
{
    public string name;
    public string url;
    public string position; // "left" or "right"


    public bool IsLeftPosition => position.Equals("left", StringComparison.OrdinalIgnoreCase);

    public override string ToString() => $"{name}({position}): {url}";
}