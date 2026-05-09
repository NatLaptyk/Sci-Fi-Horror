using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class doorlocked : MonoBehaviour
{
    
    [Header("Audio")]
    public int stingerID = 14; 


        void OnTriggerEnter(){
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayStinger(stingerID);
            }
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    

