using System.Xml.Serialization;
using UnityEngine;

public class SpriteManager : MonoBehaviour
{
    public static Sprite[] noteSprites;
    public static Sprite[] rseultSprites;
    public void Awake()
    {
         noteSprites = Resources.LoadAll<Sprite>("NoteSpriteSheet");
         rseultSprites = Resources.LoadAll<Sprite>("ResultSprites");
    }
   
}
