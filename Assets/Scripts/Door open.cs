using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Dooropen : MonoBehaviour
{
    
    [Header("Audio")]
    public int stingerID = 15; 


        void OnTriggerEnter(){
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayStinger(stingerID);
            }
        }
    }