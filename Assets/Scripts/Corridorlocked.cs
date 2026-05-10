using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Corridorlocked : MonoBehaviour
{
    
    [Header("Audio")]
    public int stingerID = 17; 


        void OnTriggerEnter(){
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayStinger(stingerID);
            }
        }
    }