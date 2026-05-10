using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Gotothedoor : MonoBehaviour
{
    
    [Header("Audio")]
    public int stingerID = 18; 


        void OnTriggerEnter(){
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayStinger(stingerID);
            }
        }
    }