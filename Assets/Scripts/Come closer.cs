using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AIWarning : MonoBehaviour
{
    
    [Header("Audio")]
    public int stingerID = 20; 


        void OnTriggerEnter(){
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayStinger(stingerID);
            }
        }
    }